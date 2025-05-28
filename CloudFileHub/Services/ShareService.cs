using CloudFileHub.Data;
using CloudFileHub.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CloudFileHub.Services;

public class ShareService
{
    private readonly ApplicationDbContext _context;
    
    public ShareService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<FileShareModel> CreateShareLinkAsync(int fileId, string userId, DateTime? expiryDate = null, 
        bool isPasswordProtected = false, string? password = null, int? maxDownloads = null)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
        if (file == null)
        {
            throw new ArgumentException("File not found or you don't have permission to share it.");
        }
        
        // Generate a unique share code
        var shareCode = GenerateUniqueShareCode();
        
        // Hash password if provided
        string? hashedPassword = null;
        if (isPasswordProtected && !string.IsNullOrEmpty(password))
        {
            hashedPassword = HashPassword(password);
        }
        
        var shareModel = new FileShareModel
        {
            FileId = fileId,
            ShareCode = shareCode,
            ExpiryDate = expiryDate,
            IsPasswordProtected = isPasswordProtected,
            Password = hashedPassword,
            MaxDownloads = maxDownloads,
            CreatedByUserId = userId
        };
        
        _context.FileShares.Add(shareModel);
        await _context.SaveChangesAsync();
        
        return shareModel;
    }
    
    public async Task<FileModel?> GetSharedFileAsync(string shareCode, string? password = null)
    {
        var share = await _context.FileShares
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.ShareCode == shareCode);
            
        if (share == null)
        {
            return null;
        }
        
        // Check if share has expired
        if (share.ExpiryDate.HasValue && share.ExpiryDate.Value < DateTime.UtcNow)
        {
            return null;
        }
        
        // Check if max downloads reached
        if (share.MaxDownloads.HasValue && share.DownloadCount >= share.MaxDownloads.Value)
        {
            return null;
        }
        
        // Verify password if required
        if (share.IsPasswordProtected)
        {
            if (string.IsNullOrEmpty(password) || !VerifyPassword(password, share.Password))
            {
                return null;
            }
        }
        
        return share.File;
    }
    
    public async Task<bool> DeleteShareLinkAsync(int shareId, string userId)
    {
        var share = await _context.FileShares
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.Id == shareId && s.CreatedByUserId == userId);
            
        if (share == null)
        {
            return false;
        }
        
        _context.FileShares.Remove(share);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<FileShareModel>> GetUserSharesAsync(string userId)
    {
        return await _context.FileShares
            .Include(s => s.File)
            .Where(s => s.CreatedByUserId == userId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();
    }
    
    public async Task IncrementDownloadCountAsync(string shareCode)
    {
        var share = await _context.FileShares.FirstOrDefaultAsync(s => s.ShareCode == shareCode);
        if (share != null)
        {
            share.DownloadCount++;
            await _context.SaveChangesAsync();
        }
    }
    
    private string GenerateUniqueShareCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var code = new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());
            
        return code;
    }
    
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
    
    private bool VerifyPassword(string inputPassword, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }
        
        var inputHash = HashPassword(inputPassword);
        return inputHash == storedHash;
    }
}
