using CloudFileHub.Models;

namespace CloudFileHub.Models.ViewModels;

public class FileDetailViewModel
{
    public FileModel File { get; set; } = null!;
    public AiAnalysisResult? AiAnalysis { get; set; }
    public string? PreviewUrl { get; set; }
    public bool CanPreview { get; set; }
    public string? ExtractedContent { get; set; }
    public List<FileModel> RelatedFiles { get; set; } = new();
} 