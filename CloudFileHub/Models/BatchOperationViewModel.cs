using System.ComponentModel.DataAnnotations;

namespace CloudFileHub.Models;

public class BatchOperationViewModel
{
    [Required]
    public string Operation { get; set; } = string.Empty; // "delete", "move"
    
    public List<int> FileIds { get; set; } = new List<int>();
    
    public List<int> DirectoryIds { get; set; } = new List<int>();
    
    public int? TargetDirectoryId { get; set; }
    
    public int? CurrentDirectoryId { get; set; }
    
    // 用于标识是否已选择目标目录（包括根目录）
    public bool HasSelectedTarget { get; set; } = false;
}
