using CloudFileHub.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace CloudFileHub.Services;

/// <summary>
/// 阿里云百炼兼容模式AI服务实现
/// 使用OpenAI兼容的API格式
/// </summary>
public class AlibabaCloudCompatibleAiService : IAiAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly AlibabaCloudAiOptions _options;
    private readonly ILogger<AlibabaCloudCompatibleAiService> _logger;

    public AlibabaCloudCompatibleAiService(
        HttpClient httpClient,
        IOptions<AlibabaCloudAiOptions> options,
        ILogger<AlibabaCloudCompatibleAiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // 设置HTTP客户端头部
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    /// <summary>
    /// 综合分析文档
    /// </summary>
    public async Task<AiAnalysisResponse> AnalyzeDocumentAsync(string content, string fileName, string contentType)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                _logger.LogWarning("阿里云百炼API密钥未配置");
                return new AiAnalysisResponse();
            }

            var prompt = CreateAnalysisPrompt(content, fileName);
            var response = await CallCompatibleApiAsync(prompt);
            
            return ParseAnalysisResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云百炼文档分析失败: {FileName}", fileName);
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
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                return new DocumentClassificationResponse { Category = "未知", Confidence = 0.0 };
            }

            var prompt = CreateClassificationPrompt(content, fileName);
            var response = await CallCompatibleApiAsync(prompt);
            
            return ParseClassificationResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云百炼文档分类失败: {FileName}", fileName);
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
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                return new List<string>();
            }

            var prompt = CreateTagGenerationPrompt(content);
            var response = await CallCompatibleApiAsync(prompt);
            
            return ParseTagsResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云百炼标签生成失败");
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
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                return ExtractFirstSentences(content, maxLength);
            }

            var prompt = CreateSummaryPrompt(content, maxLength);
            var response = await CallCompatibleApiAsync(prompt);
            
            return response?.Trim() ?? ExtractFirstSentences(content, maxLength);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云百炼摘要生成失败");
            return ExtractFirstSentences(content, maxLength);
        }
    }

    /// <summary>
    /// 调用阿里云百炼兼容模式API
    /// </summary>
    private async Task<string> CallCompatibleApiAsync(string prompt)
    {
        try
        {
            // 使用OpenAI兼容的请求格式
            var request = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "system", content = "你是一个专业的文档分析助手，能够准确分析和分类各种文档内容。请始终使用中文回复。" },
                    new { role = "user", content = prompt }
                },
                temperature = _options.Temperature,
                max_tokens = _options.MaxTokens,
                top_p = _options.TopP
            };

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            });

            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // 构建完整的API端点URL
            var apiUrl = $"{_options.Endpoint.TrimEnd('/')}/v1/chat/completions";
            var httpResponse = await _httpClient.PostAsync(apiUrl, httpContent);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                
                // 解析OpenAI兼容格式的响应
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentProp))
                    {
                        return contentProp.GetString() ?? "";
                    }
                }

                _logger.LogWarning("兼容模式API响应格式异常: {Response}", responseContent);
                return "AI服务响应异常";
            }
            else
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError("兼容模式API调用失败: {StatusCode}, {Error}", 
                    httpResponse.StatusCode, errorContent);
                return "AI服务暂时不可用";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "网络请求失败");
            return "网络连接异常";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON序列化/反序列化失败");
            return "数据格式异常";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用阿里云百炼兼容模式API失败");
            return "AI服务异常";
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
    ""category"": ""文档类别（如：技术文档、商业报告、学术论文、合同协议、用户手册等）"",
    ""tags"": [""标签1"", ""标签2"", ""标签3""],
    ""confidence"": 0.95,
    ""language"": ""zh-CN""
}}

要求：
1. 摘要要准确概括文档核心内容
2. 分类要基于文档的实际用途和内容特点
3. 标签要具体且相关，最多5个
4. 置信度要真实反映分析的确定性
5. 严格按照JSON格式返回，不要包含其他文字";
    }

    /// <summary>
    /// 创建文档分类提示词
    /// </summary>
    private string CreateClassificationPrompt(string content, string fileName)
    {
        return $@"
请对以下文档进行分类：

文件名：{fileName}

文档内容：
{content.Substring(0, Math.Min(content.Length, 3000))}

请从以下类别中选择最合适的分类，并以JSON格式返回：

主要类别：技术文档、商业报告、学术论文、合同协议、用户手册、财务报表、项目计划、会议记录、培训材料、其他

返回格式：
{{
    ""category"": ""选择的类别"",
    ""confidence"": 0.95,
    ""subcategory"": ""更具体的子类别""
}}";
    }

    /// <summary>
    /// 创建标签生成提示词
    /// </summary>
    private string CreateTagGenerationPrompt(string content)
    {
        return $@"
请为以下文档内容生成相关标签：

文档内容：
{content.Substring(0, Math.Min(content.Length, 2000))}

请生成5-10个相关标签，标签应该包括：
- 内容主题标签
- 技术/专业领域标签
- 文档类型标签

以JSON数组格式返回，例如：
[""标签1"", ""标签2"", ""标签3"", ""标签4"", ""标签5""]";
    }

    /// <summary>
    /// 创建摘要生成提示词
    /// </summary>
    private string CreateSummaryPrompt(string content, int maxLength)
    {
        return $@"
请为以下文档内容生成摘要：

文档内容：
{content.Substring(0, Math.Min(content.Length, 3000))}

要求：
1. 摘要长度不超过{maxLength}字
2. 准确概括文档的核心内容和要点
3. 使用简洁明了的语言
4. 直接返回摘要内容，不要包含其他格式";
    }

    /// <summary>
    /// 解析文档分析响应
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
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var result = JsonSerializer.Deserialize<AiAnalysisResponse>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (result != null)
                {
                    return result;
                }
            }

            // 如果JSON解析失败，返回基础分析
            _logger.LogWarning("无法解析AI分析响应，使用默认解析: {Response}", response);
            return CreateFallbackAnalysis(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析AI分析响应失败");
            return CreateFallbackAnalysis(response);
        }
    }

    /// <summary>
    /// 解析文档分类响应
    /// </summary>
    private DocumentClassificationResponse ParseClassificationResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var result = JsonSerializer.Deserialize<DocumentClassificationResponse>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (result != null)
                {
                    return result;
                }
            }

            // 简单的文本解析作为备选
            return new DocumentClassificationResponse
            {
                Category = ExtractCategoryFromText(response),
                Confidence = 0.6,
                Subcategory = ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析文档分类响应失败");
            return new DocumentClassificationResponse { Category = "其他", Confidence = 0.5 };
        }
    }

    /// <summary>
    /// 解析标签生成响应
    /// </summary>
    private List<string> ParseTagsResponse(string response)
    {
        try
        {
            // 尝试JSON数组解析
            var arrayStart = response.IndexOf('[');
            var arrayEnd = response.LastIndexOf(']');
            
            if (arrayStart >= 0 && arrayEnd > arrayStart)
            {
                var jsonText = response.Substring(arrayStart, arrayEnd - arrayStart + 1);
                var tags = JsonSerializer.Deserialize<List<string>>(jsonText);
                
                if (tags != null && tags.Count > 0)
                {
                    return tags;
                }
            }

            // 文本解析作为备选
            return ExtractTagsFromText(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析标签响应失败");
            return new List<string>();
        }
    }

    /// <summary>
    /// 创建备用分析结果
    /// </summary>
    private AiAnalysisResponse CreateFallbackAnalysis(string response)
    {
        return new AiAnalysisResponse
        {
            Summary = response.Length > 200 ? response.Substring(0, 200) + "..." : response,
            Category = "其他",
            Tags = new List<string> { "文档" },
            Confidence = 0.5,
            Language = "zh-CN"
        };
    }

    /// <summary>
    /// 从文本中提取类别
    /// </summary>
    private string ExtractCategoryFromText(string text)
    {
        var categories = new[] { "技术文档", "商业报告", "学术论文", "合同协议", "用户手册", "财务报表", "项目计划", "会议记录", "培训材料" };
        
        foreach (var category in categories)
        {
            if (text.Contains(category))
            {
                return category;
            }
        }
        
        return "其他";
    }

    /// <summary>
    /// 从文本中提取标签
    /// </summary>
    private List<string> ExtractTagsFromText(string text)
    {
        var tags = new List<string>();
        
        // 简单的关键词提取逻辑
        var keywords = new[] { "技术", "文档", "报告", "数据", "分析", "项目", "管理", "开发", "系统", "业务" };
        
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword) && !tags.Contains(keyword))
            {
                tags.Add(keyword);
            }
            
            if (tags.Count >= 5) break;
        }
        
        return tags.Count > 0 ? tags : new List<string> { "文档" };
    }

    /// <summary>
    /// 提取文档开头句子作为摘要
    /// </summary>
    private string ExtractFirstSentences(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return "";

        var sentences = content.Split(new[] { '。', '！', '？', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();
        
        foreach (var sentence in sentences.Take(3))
        {
            if (result.Length + sentence.Length > maxLength)
                break;
            
            result.Append(sentence.Trim());
            if (!sentence.EndsWith("。") && !sentence.EndsWith("."))
                result.Append("。");
        }
        
        return result.ToString();
    }
} 