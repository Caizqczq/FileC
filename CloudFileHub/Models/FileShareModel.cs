using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudFileHub.Models;

public class FileShareModel
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int FileId { get; set; }
    
    [ForeignKey("FileId")]
    public FileModel File { get; set; } = null!;
    
    [Required]
    [StringLength(50)]
    public string ShareCode { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiryDate { get; set; }
    
    public bool IsPasswordProtected { get; set; } = false;
    
    [StringLength(100)]
    public string? Password { get; set; }
    
    public int DownloadCount { get; set; } = 0;
    
    public int? MaxDownloads { get; set; }
    
    public string CreatedByUserId { get; set; } = string.Empty;
}
