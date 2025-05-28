using Microsoft.AspNetCore.Identity;

namespace CloudFileHub.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long StorageUsed { get; set; } = 0;
    public long StorageLimit { get; set; } = 1073741824; // 1GB default
}
