using System.Text.Json.Serialization;

namespace CloudFileHub.Models;

/// <summary>
/// 阿里云百炼配置选项
/// </summary>
public class AlibabaCloudAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
    public string Model { get; set; } = "qwen-turbo"; // 或 qwen-plus, qwen-max
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2048;
    public double TopP { get; set; } = 0.8;
    public string Provider { get; set; } = "AlibabaCloud";
}

/// <summary>
/// 阿里云百炼API请求模型
/// </summary>
public class DashScopeRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public DashScopeInput Input { get; set; } = new();

    [JsonPropertyName("parameters")]
    public DashScopeParameters Parameters { get; set; } = new();
}

/// <summary>
/// 输入数据
/// </summary>
public class DashScopeInput
{
    [JsonPropertyName("messages")]
    public List<DashScopeMessage> Messages { get; set; } = new();
}

/// <summary>
/// 消息对象
/// </summary>
public class DashScopeMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // system, user, assistant

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 请求参数
/// </summary>
public class DashScopeParameters
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2048;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 0.8;

    [JsonPropertyName("result_format")]
    public string ResultFormat { get; set; } = "text";
}

/// <summary>
/// 阿里云百炼API响应模型
/// </summary>
public class DashScopeResponse
{
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("output")]
    public DashScopeOutput? Output { get; set; }

    [JsonPropertyName("usage")]
    public DashScopeUsage? Usage { get; set; }
}

/// <summary>
/// 响应输出
/// </summary>
public class DashScopeOutput
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<DashScopeChoice>? Choices { get; set; }
}

/// <summary>
/// 选择项
/// </summary>
public class DashScopeChoice
{
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public DashScopeMessage? Message { get; set; }
}

/// <summary>
/// 使用统计
/// </summary>
public class DashScopeUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
} 