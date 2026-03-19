# Octology WhatsApp AI Assistant (ASP.NET Core + Ollama)

Backend starter for a WhatsApp AI assistant using:

- ASP.NET Core 8 Web API
- PostgreSQL (Entity Framework Core)
- Ollama (local LLM)
- Meta WhatsApp Cloud API

## Architecture Overview

- **Controllers**
  - `WhatsAppWebhookController`: Handles WhatsApp webhook verification and incoming messages.
  - `LeadsController`: Exposes basic read endpoints for captured leads.
  - `ChatbotKeywordsController`, `ChatbotSettingsController`, `ChatbotFaqController`: Admin API for configurable chatbot behaviour.
- **Services**
  - `MessageProcessorService`: Orchestrates message flow (greeting → FAQ → domain keywords → out-of-scope or Ollama).
  - `OllamaService`: Calls the local Ollama API (system prompt configurable via settings).
  - `WhatsAppService`: Sends outbound messages via WhatsApp Cloud API.
  - `KeywordService`, `SettingsService`, `FaqService`: Configurable keywords, settings (greeting, out-of-scope, system prompt), and FAQs (cached 5 min).
  - `LeadService`, `ConversationService`: User and conversation persistence.
- **Data**
  - `AppDbContext`: EF Core DbContext for PostgreSQL.
- **Models**
  - `User`, `Conversation`, `Lead`, `ChatbotKeyword`, `ChatbotSetting`, `ChatbotFAQ`.
- **DTOs**
  - `WhatsAppWebhookDto`, `WhatsAppMessageDto`.
- **Config**
  - `WhatsAppSettings`, `OllamaSettings` bound from `appsettings.json`.

Message flow:

1. WhatsApp sends webhook to `POST /webhook/whatsapp`.
2. Payload is parsed; phone number and message are extracted.
3. `MessageProcessorService` (configurable from admin panel):
   - Gets/creates `User`.
   - If message is greeting (“hi”/“hello”) → return **GREETING_MESSAGE** from settings.
   - Else if message matches an FAQ trigger → return that FAQ response.
   - Else if domain keywords are configured and message contains none → return **OUT_OF_SCOPE_REPLY** from settings.
   - Else call Ollama (using **SYSTEM_PROMPT** from settings if set).
   - Persists `Conversation` and (if freight/quote) `Lead`; sends reply via WhatsApp.

## Prerequisites

- .NET 8 SDK
- PostgreSQL
- Ollama installed locally
- Meta WhatsApp Cloud API app + phone number ID + access token

## Setup: Ollama

1. Install Ollama from the official site.
2. Pull the `mistral` model:

   ```bash
   ollama pull mistral
   ```

3. Run Ollama server:

   ```bash
   ollama serve
   ```

   By default it listens on `http://localhost:11434`.
   To check run "Invoke-WebRequest -Uri "http://localhost:11434" -UseBasicParsing" in powershell

## Setup: Database (PostgreSQL)

1. Create a PostgreSQL database, e.g. `octology_whatsapp`.

2. Update the connection string in `appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=octology_whatsapp;Username=postgres;Password=yourpassword"
   }
   ```

3. Add EF Core tools (globally or via local dotnet tool) if needed:

   ```bash
   dotnet tool install --global dotnet-ef
   ```

4. From the project root, create and apply migrations:

   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

   The app also runs `Database.Migrate()` on startup, so schema updates are applied automatically when the app boots (suitable for dev/staging).

## Setup: WhatsApp Cloud API

1. In Meta Developer console, create a WhatsApp Cloud API app and get:
   - **Phone Number ID**
   - **Permanent Access Token** (or long-lived token)
2. In `appsettings.json`, configure:

   ```json
   "WhatsApp": {
     "AccessToken": "YOUR_META_WHATSAPP_ACCESS_TOKEN",
     "PhoneNumberId": "YOUR_PHONE_NUMBER_ID",
     "VerifyToken": "YOUR_VERIFY_TOKEN"
   }
   ```

   - `VerifyToken` is an arbitrary secret string you choose; you must use the same value in the Meta console webhook configuration.

## Running the API

From the project root:

```bash
dotnet restore
dotnet run
```

By default, Kestrel will listen on `http://localhost:5000` and `https://localhost:5001` (depending on your ASP.NET Core profile).

Swagger UI is available in development at:

```text
https://localhost:5001/swagger
```

## Exposing Webhook with ngrok

Meta requires a publicly reachable HTTPS URL for the webhook.

1. Install ngrok and authenticate with your ngrok account.

2. Expose your ASP.NET Core app (assuming HTTP on port 5000):

   ```bash
   ngrok http http://localhost:5000
   ```

3. Note the public HTTPS URL from ngrok, e.g. `https://abcd1234.ngrok.io`.

4. In the Meta Developer console:
   - Configure the webhook URL as:

     ```text
     https://abcd1234.ngrok.io/webhook
     ```

   - Set the **Verify Token** to the same value as `WhatsApp:VerifyToken` in `appsettings.json`.

5. Meta will call `GET /webhook` for verification. The API compares `hub.verify_token` with your configured verify token and returns `hub.challenge` when valid.

## Webhook Endpoints

- **Verify Webhook**

  - `GET /webhook`
  - Query params:
    - `hub.mode`
    - `hub.verify_token`
    - `hub.challenge`
  - Behavior:
    - If `hub.verify_token` matches config, returns `hub.challenge`.
    - Otherwise, returns `401 Unauthorized`.

- **Receive Messages**

  - `POST /webhook/whatsapp`
  - Expected payload (simplified example):

    ```json
    {
      "entry": [
        {
          "changes": [
            {
              "value": {
                "messages": [
                  {
                    "from": "919999999999",
                    "text": {
                      "body": "Hi"
                    }
                  }
                ]
              }
            }
          ]
        }
      ]
    }
    ```

  - Extracts:
    - Phone number from `messages[0].from`
    - Text from `messages[0].text.body`

## Conversation Logic

- **Greeting detection**
  - If message contains `"hi"` or `"hello"` (case-insensitive), the bot replies with:

    ```text
    Welcome to Octology Logistics Assistant 🚢

    1 Track container
    2 Freight quote
    3 Import documentation
    4 Talk to agent
    ```

  - No Ollama call is required for simple greetings.

- **Freight quote detection**
  - If message contains `"freight"` or `"quote"` (case-insensitive), a `Lead` is saved:
    - `PhoneNumber` from the sender
    - `Requirement` from the message text

- **General questions**
  - For other messages, `OllamaService` sends a prompt:
    - System prompt from setting **SYSTEM_PROMPT** (or default Octology logistics assistant).
    - User message is appended.
  - Response is saved in `Conversation` and sent back via WhatsApp.

Greeting text, out-of-scope reply, and system prompt are configurable via **ChatbotSettings** (see below).

## Configurable Chatbot (Admin API)

Behaviour is driven by PostgreSQL and can be managed from the admin panel or Swagger.

### Settings (`ChatbotSettings`)

- **GET /api/chatbot/settings** — list all key/value settings.
- **PUT /api/chatbot/settings** — upsert settings. Body: `{ "settings": [ { "settingKey": "GREETING_MESSAGE", "settingValue": "..." }, ... ] }`.

Recommended keys:

- `OUT_OF_SCOPE_REPLY` — reply when the user message does not contain any domain keyword.
- `GREETING_MESSAGE` — reply for “hi” / “hello”.
- `SYSTEM_PROMPT` — system prompt sent to Ollama (optional).

### Domain keywords (`ChatbotKeywords`)

- **GET /api/chatbot/keywords** — list all keywords.
- **POST /api/chatbot/keywords** — add keyword. Body: `{ "keyword": "shipment", "isActive": true }`.
- **DELETE /api/chatbot/keywords/{id}** — remove keyword.

If at least one keyword exists, messages that contain none of them receive `OUT_OF_SCOPE_REPLY`. If no keywords are configured, all messages are treated as in-scope and go to the LLM.

### FAQs (`ChatbotFAQs`)

- **GET /api/chatbot/faqs** — list all FAQs.
- **POST /api/chatbot/faqs** — add FAQ. Body: `{ "triggerText": "track container", "responseText": "You can track at ...", "isActive": true }`.
- **DELETE /api/chatbot/faqs/{id}** — remove FAQ.

If the user message contains `triggerText` (case-insensitive), the bot replies with `responseText` and does not call the LLM.

Config (keywords, settings, FAQs) is cached in memory for 5 minutes; changes take effect after cache expiry or restart.

## Data Model

- **User**
  - `Id` (int, identity)
  - `PhoneNumber` (unique)
  - `CreatedDate`

- **Conversation**
  - `Id`
  - `UserId` (FK to `User`)
  - `UserMessage`
  - `BotResponse`
  - `Timestamp`

- **Lead**
  - `Id`
  - `PhoneNumber`
  - `Requirement`
  - `CreatedDate`

- **ChatbotKeyword** — `Id` (Guid), `Keyword`, `IsActive`, `CreatedAt`
- **ChatbotSetting** — `Id` (Guid), `SettingKey`, `SettingValue`
- **ChatbotFAQ** — `Id` (Guid), `TriggerText`, `ResponseText`, `IsActive`, `CreatedAt`

## Leads API

- `GET /api/leads` — list all leads.
- `GET /api/leads/{id}` — get a single lead.

Use this to integrate with CRM or BI pipelines.

## Logging

The project uses structured logging via `ILogger`:

- Incoming message and final response.
- Greeting detection, FAQ match, keyword (in/out of scope) detection.
- LLM calls and responses.
- Outbound WhatsApp API calls.
- Database operations for conversations and leads.

Configure log levels in `appsettings.json` under `Logging`.

