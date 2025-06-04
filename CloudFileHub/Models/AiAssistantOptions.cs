namespace CloudFileHub.Models;

public class AiAssistantOptions
{
    public const string SectionName = "AiAssistant";
    
    public string Provider { get; set; } = "OpenAI"; // OpenAI, QianWen, Azure
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.3;
} 