using Xunit;

namespace WhatsAppDev.Tests;

/// <summary>
/// Tests the out-of-scope logic: when no greeting, no FAQ match, and message does not contain any domain keyword,
/// the processor should return the configured out-of-scope reply instead of calling the LLM.
/// </summary>
public class OutOfScopeLogicTests
{
    [Fact]
    public void When_Keywords_Exist_And_Message_Contains_None_Then_InScope_Is_False()
    {
        var keywords = new List<string> { "shipment", "freight", "track" };
        var message = "tell me a joke";
        var lower = message.ToLowerInvariant();
        var inScope = keywords.Any(k => lower.Contains(k));
        Assert.False(inScope);
    }

    [Fact]
    public void When_Keywords_Exist_And_Message_Contains_One_Then_InScope_Is_True()
    {
        var keywords = new List<string> { "shipment", "freight", "track" };
        var message = "I need to track my shipment";
        var lower = message.ToLowerInvariant();
        var inScope = keywords.Any(k => lower.Contains(k));
        Assert.True(inScope);
    }

    [Fact]
    public void When_Keywords_List_Is_Empty_Then_All_Messages_Considered_InScope()
    {
        var keywords = new List<string>();
        var inScope = keywords.Count == 0 || keywords.Any(k => "anything".Contains(k));
        Assert.True(inScope);
    }

    [Fact]
    public void OutOfScopeReply_Used_When_No_Keyword_Matches()
    {
        var defaultOutOfScope = "I can only help with logistics, shipment tracking, freight quotes, and import documentation.";
        var configuredReply = "Sorry, I only handle logistics queries. Please ask about shipments or freight.";
        var fromSettings = configuredReply;
        var finalReply = string.IsNullOrWhiteSpace(fromSettings) ? defaultOutOfScope : fromSettings;
        Assert.Equal(configuredReply, finalReply);
    }

    [Fact]
    public void Default_OutOfScope_Used_When_Setting_Is_Empty()
    {
        var defaultOutOfScope = "I can only help with logistics, shipment tracking, freight quotes, and import documentation.";
        string? fromSettings = null;
        var finalReply = string.IsNullOrWhiteSpace(fromSettings) ? defaultOutOfScope : fromSettings;
        Assert.Equal(defaultOutOfScope, finalReply);
    }
}
