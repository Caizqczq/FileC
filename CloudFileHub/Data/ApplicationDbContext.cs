 using CloudFileHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudFileHub.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<FileModel> Files { get; set; } = null!;
    public DbSet<DirectoryModel> Directories { get; set; } = null!;
    public DbSet<FileShareModel> FileShares { get; set; } = null!;
    public DbSet<AiAnalysisResult> AiAnalysisResults { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure relationships
        builder.Entity<DirectoryModel>()
            .HasMany(d => d.Subdirectories)
            .WithOne(d => d.Parent)
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<DirectoryModel>()
            .HasMany(d => d.Files)
            .WithOne(f => f.Directory)
            .HasForeignKey(f => f.DirectoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<FileModel>()
            .HasMany(f => f.Shares)
            .WithOne(s => s.File)
            .HasForeignKey(s => s.FileId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<AiAnalysisResult>()
            .HasOne(a => a.File)
            .WithMany()
            .HasForeignKey(a => a.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}