using Azure.AI.OpenAI;
using Azure;
using CloudFileHub.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using OpenAI.Chat;
using System.ClientModel;

namespace CloudFileHub.Services;

/// <summary>
/// AI助手服务接口
/// </summary>
public interface IAiAssistantService
{
    Task<AiAnalysisResponse> AnalyzeDocumentAsync(string content, string fileName, string contentType);
    Task<DocumentClassificationResponse> ClassifyDocumentAsync(string content, string fileName);
    Task<List<string>> GenerateTagsAsync(string content);
    Task<string> GenerateSummaryAsync(string content, int maxLength = 200);
}

/// <summary>
/// AI助手服务实现
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly AzureOpenAIClient? _openAiClient;
    private readonly AiAssistantOptions _options;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        IOptions<AiAssistantOptions> options,
        ILogger<AiAssistantService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            if (_options.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                _openAiClient = new AzureOpenAIClient(new Uri(_options.Endpoint), new ApiKeyCredential(_options.ApiKey));
            }
            else
            {
                _openAiClient = new AzureOpenAIClient(new Uri("https://api.openai.com/v1"), new ApiKeyCredential(_options.ApiKey));
            }
        }
        else
        {
            _logger.LogWarning("AI服务未配置API密钥，AI功能将不可用");
        }
    }

    /// <summary>
    /// 综合分析文档
    /// </summary>
    public async Task<AiAnalysisResponse> AnalyzeDocumentAsync(string content, string fileName, string contentType)
    {
        try
        {
            if (_openAiClient == null)
            {
                _logger.LogWarning("AI客户端未初始化");
                return new AiAnalysisResponse();
            }

            var prompt = CreateAnalysisPrompt(content, fileName);
            var response = await CallOpenAIAsync(prompt);
            
            return ParseAnalysisResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI文档分析失败: {FileName}", fileName);
            return new AiAnalysisResponse();
        }
    }

    /// <summary>
    /// 文档分类
    /// </summary>
    public async Task<DocumentClassificationResponse> ClassifyDocumentAsync(string content, string fileName)
    {
        try
        {
            if (_openAiClient == null)
            {
                return new DocumentClassificationResponse { Category = "未知", Confidence = 0.0 };
            }

            var prompt = CreateClassificationPrompt(content, fileName);
            var response = await CallOpenAIAsync(prompt);
            
            return ParseClassificationResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文档分类失败: {FileName}", fileName);
            return new DocumentClassificationResponse { Category = "未知", Confidence = 0.0 };
        }
    }

    /// <summary>
    /// 生成标签
    /// </summary>
    public async Task<List<string>> GenerateTagsAsync(string content)
    {
        try
        {
            if (_openAiClient == null)
            {
                return new List<string>();
            }

            var prompt = CreateTagGenerationPrompt(content);
            var response = await CallOpenAIAsync(prompt);
            
            return ParseTagsResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标签生成失败");
            return new List<string>();
        }
    }

    /// <summary>
    /// 生成摘要
    /// </summary>
    public async Task<string> GenerateSummaryAsync(string content, int maxLength = 200)
    {
        try
        {
            if (_openAiClient == null)
            {
                return ExtractFirstSentences(content, maxLength);
            }

            var prompt = CreateSummaryPrompt(content, maxLength);
            var response = await CallOpenAIAsync(prompt);
            
            return response?.Trim() ?? ExtractFirstSentences(content, maxLength);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "摘要生成失败");
            return ExtractFirstSentences(content, maxLength);
        }
    }

    /// <summary>
    /// 调用OpenAI API
    /// </summary>
    private async Task<string> CallOpenAIAsync(string prompt)
    {
        try
        {
            if (_openAiClient == null)
            {
                return "AI服务未初始化";
            }
            
            var chatClient = _openAiClient.GetChatClient(_options.Model);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("你是一个专业的文档分析助手，能够准确分析和分类各种文档内容。请始终使用中文回复。"),
                new UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
            {
                Temperature = (float)_options.Temperature
            });

            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用OpenAI API失败");
            return "AI服务暂时不可用";
        }
    }

    /// <summary>
    /// 创建文档分析提示词
    /// </summary>
    private string CreateAnalysisPrompt(string content, string fileName)
    {
        return $@"
请分析以下文档内容，并以JSON格式返回分析结果：

文件名：{fileName}

文档内容：
{content.Substring(0, Math.Min(content.Length, 4000))}

请返回以下格式的JSON：
{{
    ""summary"": ""文档摘要（150字以内）"",
    ""category"": ""文档类别（如：合同、报告、简历、说明书、发票等）"",
    ""tags"": [""标签1"", ""标签2"", ""标签3""],
    ""confidence"": 0.85,
    ""language"": ""文档主要语言""
}}

请确保返回有效的JSON格式。";
    }

    /// <summary>
    /// 创建文档分类提示词
    /// </summary>
    private string CreateClassificationPrompt(string content, string fileName)
    {
        return $@"
请对以下文档进行分类，从以下类别中选择最合适的：
- 合同文件
- 财务报告
- 技术文档
- 个人简历
- 商业提案
- 学术论文
- 操作手册
- 法律文件
- 通知公告
- 其他

文件名：{fileName}
文档内容：{content.Substring(0, Math.Min(content.Length, 2000))}

请以JSON格式返回：
{{
    ""category"": ""选择的类别"",
    ""confidence"": 0.85,
    ""subcategory"": ""子类别（可选）""
}}";
    }

    /// <summary>
    /// 创建标签生成提示词
    /// </summary>
    private string CreateTagGenerationPrompt(string content)
    {
        return $@"
请为以下文档内容生成3-5个相关标签，标签应该简洁、准确地描述文档的主要特征：

文档内容：
{content.Substring(0, Math.Min(content.Length, 2000))}

请返回JSON格式的标签列表：
{{
    ""tags"": [""标签1"", ""标签2"", ""标签3""]
}}";
    }

    /// <summary>
    /// 创建摘要生成提示词
    /// </summary>
    private string CreateSummaryPrompt(string content, int maxLength)
    {
        return $@"
请为以下文档生成一个简洁的摘要，摘要长度不超过{maxLength}个字符：

文档内容：
{content.Substring(0, Math.Min(content.Length, 3000))}

请直接返回摘要内容，不需要JSON格式。";
    }

    /// <summary>
    /// 解析AI分析响应
    /// </summary>
    private AiAnalysisResponse ParseAnalysisResponse(string response)
    {
        try
        {
            // 尝试提取JSON部分
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<AiAnalysisResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new AiAnalysisResponse();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析AI响应失败: {Response}", response);
        }

        // 解析失败时返回默认值
        return new AiAnalysisResponse
        {
            Summary = "AI分析暂时不可用",
            Category = "未分类",
            Tags = new List<string> { "文档" },
            Confidence = 0.5,
            Language = "中文"
        };
    }

    /// <summary>
    /// 解析分类响应
    /// </summary>
    private DocumentClassificationResponse ParseClassificationResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<DocumentClassificationResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new DocumentClassificationResponse();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析分类响应失败: {Response}", response);
        }

        return new DocumentClassificationResponse { Category = "其他", Confidence = 0.5 };
    }

    /// <summary>
    /// 解析标签响应
    /// </summary>
    private List<string> ParseTagsResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var tagResponse = JsonSerializer.Deserialize<TagGenerationResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return tagResponse?.Tags?.Select(t => t.Name).ToList() ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析标签响应失败: {Response}", response);
        }

        return new List<string> { "文档" };
    }

    /// <summary>
    /// 提取前几句作为备用摘要
    /// </summary>
    private string ExtractFirstSentences(string content, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "无内容";

        var sentences = content.Split(new[] { '。', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var summary = "";
        
        foreach (var sentence in sentences)
        {
            if (summary.Length + sentence.Length > maxLength)
                break;
            summary += sentence.Trim() + "。";
        }

        return string.IsNullOrWhiteSpace(summary) ? content.Substring(0, Math.Min(content.Length, maxLength)) : summary;
    }
} 