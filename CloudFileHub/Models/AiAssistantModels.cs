using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CloudFileHub.Models;

/// <summary>
/// AI分析请求模型
/// </summary>
public class AiAnalysisRequest
{
    public string Content { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// AI分析响应模型
/// </summary>
public class AiAnalysisResponse
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;
}

/// <summary>
/// 文档分类响应
/// </summary>
public class DocumentClassificationResponse
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
    
    [JsonPropertyName("subcategory")]
    public string Subcategory { get; set; } = string.Empty;
}

/// <summary>
/// 标签生成响应
/// </summary>
public class TagGenerationResponse
{
    [JsonPropertyName("tags")]
    public List<TagInfo> Tags { get; set; } = new List<TagInfo>();
}

/// <summary>
/// 标签信息
/// </summary>
public class TagInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("relevance")]
    public double Relevance { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // content, format, technical, business
}

/// <summary>
/// AI分析结果实体
/// </summary>
public class AiAnalysisResult
{
    [Key]
    public int Id { get; set; }
    
    public int FileId { get; set; }
    
    [StringLength(2000)]
    public string Summary { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Tags { get; set; } = string.Empty; // JSON格式存储
    
    public double Confidence { get; set; }
    
    [StringLength(50)]
    public string Language { get; set; } = string.Empty;
    
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    
    public string? ExtractedContent { get; set; }
    
    // 导航属性
    public FileModel? File { get; set; }
} 