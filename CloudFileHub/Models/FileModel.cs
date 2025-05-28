using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudFileHub.Models;

public class FileModel
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [StringLength(1000)]
    public string FilePath { get; set; } = string.Empty;
    
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    [StringLength(255)]
    public string? Description { get; set; }
    
    public bool IsPublic { get; set; } = false;
    
    public string? UserId { get; set; }
    
    public int? DirectoryId { get; set; }
    
    [ForeignKey("DirectoryId")]
    public DirectoryModel? Directory { get; set; }
    
    public ICollection<FileShareModel> Shares { get; set; } = new List<FileShareModel>();
}
