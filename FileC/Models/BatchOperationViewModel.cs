using System.ComponentModel.DataAnnotations;

namespace FileC.Models;

public class BatchOperationViewModel
{
    [Required]
    public string Operation { get; set; } = string.Empty; // "delete", "move"
    
    public List<int> FileIds { get; set; } = new List<int>();
    
    public List<int> DirectoryIds { get; set; } = new List<int>();
    
    public int? TargetDirectoryId { get; set; }
    
    public int? CurrentDirectoryId { get; set; }
}
