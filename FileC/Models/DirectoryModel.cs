using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileC.Models;

public class DirectoryModel
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public int? ParentId { get; set; }
    
    [ForeignKey("ParentId")]
    public DirectoryModel? Parent { get; set; }
    
    public string? UserId { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public bool IsPublic { get; set; } = false;
    
    public ICollection<DirectoryModel> Subdirectories { get; set; } = new List<DirectoryModel>();
    
    public ICollection<FileModel> Files { get; set; } = new List<FileModel>();
}
