using CloudFileHub.Models;
using Microsoft.Extensions.Options;

namespace CloudFileHub.Services;

/// <summary>
/// AI服务工厂，根据配置选择不同的AI提供商
/// </summary>
public class AiServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AiAssistantOptions _azureOptions;
    private readonly AlibabaCloudAiOptions _alibabaOptions;
    private readonly ILogger<AiServiceFactory> _logger;

    public AiServiceFactory(
        IServiceProvider serviceProvider,
        IOptions<AiAssistantOptions> azureOptions,
        IOptions<AlibabaCloudAiOptions> alibabaOptions,
        ILogger<AiServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _azureOptions = azureOptions.Value;
        _alibabaOptions = alibabaOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// 创建AI助手服务实例
    /// </summary>
    /// <param name="preferredProvider">首选提供商 (Azure/AlibabaCloud)</param>
    /// <returns>AI助手服务实例</returns>
    public IAiAssistantService CreateAiService(string? preferredProvider = null)
    {
        try
        {
            // 如果指定了首选提供商，优先使用
            if (!string.IsNullOrEmpty(preferredProvider))
            {
                if (preferredProvider.Equals("AlibabaCloud", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(_alibabaOptions.ApiKey))
                {
                    _logger.LogInformation("使用阿里云百炼AI服务");
                    return _serviceProvider.GetRequiredService<AlibabaCloudAiService>();
                }
                else if (preferredProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(_azureOptions.ApiKey))
                {
                    _logger.LogInformation("使用Azure OpenAI服务");
                    return _serviceProvider.GetRequiredService<AiAssistantService>();
                }
            }

            // 自动选择可用的服务
            // 优先选择阿里云百炼（如果配置了）
            if (!string.IsNullOrEmpty(_alibabaOptions.ApiKey))
            {
                _logger.LogInformation("自动选择阿里云百炼AI服务");
                return _serviceProvider.GetRequiredService<AlibabaCloudAiService>();
            }
            
            // 备选Azure OpenAI
            if (!string.IsNullOrEmpty(_azureOptions.ApiKey))
            {
                _logger.LogInformation("自动选择Azure OpenAI服务");
                return _serviceProvider.GetRequiredService<AiAssistantService>();
            }

            // 如果都没有配置，返回空实现
            _logger.LogWarning("没有配置可用的AI服务，返回空实现");
            return new NullAiAssistantService(_logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建AI服务失败");
            return new NullAiAssistantService(_logger);
        }
    }

    /// <summary>
    /// 检查服务可用性
    /// </summary>
    public (bool IsAlibabaAvailable, bool IsAzureAvailable) CheckServiceAvailability()
    {
        var alibabaAvailable = !string.IsNullOrEmpty(_alibabaOptions.ApiKey);
        var azureAvailable = !string.IsNullOrEmpty(_azureOptions.ApiKey);
        
        return (alibabaAvailable, azureAvailable);
    }
}

/// <summary>
/// 空AI服务实现，用于没有配置任何AI服务时的降级处理
/// </summary>
public class NullAiAssistantService : IAiAssistantService
{
    private readonly ILogger _logger;

    public NullAiAssistantService(ILogger logger)
    {
        _logger = logger;
    }

    public Task<AiAnalysisResponse> AnalyzeDocumentAsync(string content, string fileName, string contentType)
    {
        _logger.LogWarning("AI服务未配置，返回基础分析结果");
        
        return Task.FromResult(new AiAnalysisResponse
        {
            Summary = ExtractFirstSentences(content, 200),
            Category = GetBasicCategory(fileName),
            Tags = new List<string> { "文档", "未分析" },
            Confidence = 0.3,
            Language = "zh-CN"
        });
    }

    public Task<DocumentClassificationResponse> ClassifyDocumentAsync(string content, string fileName)
    {
        return Task.FromResult(new DocumentClassificationResponse
        {
            Category = GetBasicCategory(fileName),
            Confidence = 0.3,
            Subcategory = ""
        });
    }

    public Task<List<string>> GenerateTagsAsync(string content)
    {
        return Task.FromResult(new List<string> { "文档", "未分析" });
    }

    public Task<string> GenerateSummaryAsync(string content, int maxLength = 200)
    {
        return Task.FromResult(ExtractFirstSentences(content, maxLength));
    }

    private string GetBasicCategory(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".pdf" => "PDF文档",
            ".docx" or ".doc" => "Word文档",
            ".txt" => "文本文件",
            ".xlsx" or ".xls" => "Excel文档",
            ".pptx" or ".ppt" => "PowerPoint文档",
            _ => "其他文档"
        };
    }

    private string ExtractFirstSentences(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return "";

        if (content.Length <= maxLength)
            return content;

        var truncated = content.Substring(0, maxLength);
        var lastSentenceEnd = Math.Max(
            Math.Max(truncated.LastIndexOf('。'), truncated.LastIndexOf('.')),
            Math.Max(truncated.LastIndexOf('!'), truncated.LastIndexOf('?'))
        );

        if (lastSentenceEnd > 0)
            return truncated.Substring(0, lastSentenceEnd + 1);

        return truncated + "...";
    }
} 