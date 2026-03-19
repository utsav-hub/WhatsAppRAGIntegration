using Microsoft.EntityFrameworkCore;
using Pgvector;
using WhatsAppDev.Services;
using WhatsAppDev.Config;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsApp"));
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));

// Logging is configured by default; ensure console logging enabled
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

// HttpClient factories
builder.Services.AddHttpClient("Ollama");
builder.Services.AddHttpClient("WhatsAppGraphApi");

// Memory cache for chatbot config and responses
builder.Services.AddMemoryCache();

// Application services
builder.Services.AddScoped<OllamaService>();
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<LeadService>();
builder.Services.AddScoped<KeywordService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<FaqService>();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<TextChunkingService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<VectorSearchService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<MessageProcessorService>();

// Knowledge ingestion (file upload -> background extraction -> chunk+embed)
builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();
builder.Services.AddScoped<PdfTextExtractionService>();
builder.Services.AddScoped<DocxTextExtractionService>();
builder.Services.AddHostedService<KnowledgeIngestionWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for admin React app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminUiCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply pending migrations and seed default chatbot keywords if empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!await db.ChatbotKeywords.AnyAsync())
    {
        var defaultKeywords = new[]
        {
            "shipment", "ship", "shipping", "freight", "cargo", "container", "track", "tracking",
            "logistics", "import", "export", "customs", "bill of lading", "bl", "incoterms",
            "fob", "cif", "fcl", "lcl", "warehouse", "delivery", "transit", "port", "harbor",
            "quote", "rate", "tariff", "documentation", "clearance", "inbound", "outbound"
        };
        foreach (var kw in defaultKeywords)
        {
            db.ChatbotKeywords.Add(new ChatbotKeyword
            {
                Id = Guid.NewGuid(),
                Keyword = kw,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AdminUiCors");

app.MapControllers();

app.Run();

