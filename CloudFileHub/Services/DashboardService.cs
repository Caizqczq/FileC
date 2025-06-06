using CloudFileHub.Data;
using CloudFileHub.Models;
using CloudFileHub.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CloudFileHub.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户仪表盘数据
    /// </summary>
    public async Task<DashboardViewModel> GetDashboardDataAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new DashboardViewModel();
            }

            var dashboard = new DashboardViewModel
            {
                UserStats = await GetUserStatsAsync(userId, user),
                RecentFiles = await GetRecentFilesAsync(userId),
                RecentAiAnalyzedFiles = await GetRecentAiAnalyzedFilesAsync(userId),
                RecentShares = await GetRecentSharesAsync(userId),
                StorageStats = await GetStorageStatsAsync(userId),
                ActivityStats = await GetActivityStatsAsync(userId)
            };

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仪表盘数据失败，用户ID: {UserId}", userId);
            return new DashboardViewModel();
        }
    }

    private async Task<UserStatsViewModel> GetUserStatsAsync(string userId, ApplicationUser user)
    {
        var totalFiles = await _context.Files.CountAsync(f => f.UserId == userId);
        var totalDirectories = await _context.Directories.CountAsync(d => d.UserId == userId);
        var totalShares = await _context.FileShares
            .Include(s => s.File)
            .CountAsync(s => s.CreatedByUserId == userId);
        var filesWithAi = await _context.Files
            .CountAsync(f => f.UserId == userId && !string.IsNullOrEmpty(f.AiSummary));

        return new UserStatsViewModel
        {
            TotalFiles = totalFiles,
            TotalDirectories = totalDirectories,
            TotalStorageUsed = user.StorageUsed,
            StorageLimit = user.StorageLimit,
            TotalShares = totalShares,
            FilesWithAiAnalysis = filesWithAi
        };
    }

    private async Task<List<FileModel>> GetRecentFilesAsync(string userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadDate)
            .Take(5)
            .ToListAsync();
    }

    private async Task<List<FileModel>> GetRecentAiAnalyzedFilesAsync(string userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId && !string.IsNullOrEmpty(f.AiSummary))
            .OrderByDescending(f => f.LastAiAnalysis)
            .Take(5)
            .ToListAsync();
    }

    private async Task<List<FileShareModel>> GetRecentSharesAsync(string userId)
    {
        return await _context.FileShares
            .Include(s => s.File)
            .Where(s => s.CreatedByUserId == userId)
            .OrderByDescending(s => s.CreatedDate)
            .Take(5)
            .ToListAsync();
    }

    private async Task<StorageStatsViewModel> GetStorageStatsAsync(string userId)
    {
        var files = await _context.Files
            .Where(f => f.UserId == userId)
            .Select(f => new { f.ContentType, f.FileSize })
            .ToListAsync();

        var storageStats = new StorageStatsViewModel();

        foreach (var file in files)
        {
            var category = GetFileCategory(file.ContentType);
            
            if (storageStats.FileTypeStorage.ContainsKey(category))
            {
                storageStats.FileTypeStorage[category] += file.FileSize;
                storageStats.FileTypeCount[category]++;
            }
            else
            {
                storageStats.FileTypeStorage[category] = file.FileSize;
                storageStats.FileTypeCount[category] = 1;
            }
        }

        return storageStats;
    }

    private async Task<ActivityStatsViewModel> GetActivityStatsAsync(string userId)
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var filesUploadedToday = await _context.Files
            .CountAsync(f => f.UserId == userId && f.UploadDate.Date == today);

        var filesUploadedThisWeek = await _context.Files
            .CountAsync(f => f.UserId == userId && f.UploadDate >= weekStart);

        var filesUploadedThisMonth = await _context.Files
            .CountAsync(f => f.UserId == userId && f.UploadDate >= monthStart);

        var sharesCreatedThisWeek = await _context.FileShares
            .Include(s => s.File)
            .CountAsync(s => s.CreatedByUserId == userId && s.CreatedDate >= weekStart);

        var aiAnalysisThisWeek = await _context.Files
            .CountAsync(f => f.UserId == userId && 
                           f.LastAiAnalysis.HasValue && 
                           f.LastAiAnalysis.Value >= weekStart);

        return new ActivityStatsViewModel
        {
            FilesUploadedToday = filesUploadedToday,
            FilesUploadedThisWeek = filesUploadedThisWeek,
            FilesUploadedThisMonth = filesUploadedThisMonth,
            SharesCreatedThisWeek = sharesCreatedThisWeek,
            AiAnalysisThisWeek = aiAnalysisThisWeek,
            LastLoginDate = DateTime.UtcNow // 可以后续实现真实的最后登录时间追踪
        };
    }

    private static string GetFileCategory(string contentType)
    {
        return contentType.ToLower() switch
        {
            var ct when ct.StartsWith("image/") => "图片",
            var ct when ct.Contains("pdf") => "PDF文档",
            var ct when ct.Contains("word") || ct.Contains("document") => "Word文档",
            var ct when ct.Contains("excel") || ct.Contains("sheet") => "Excel表格",
            var ct when ct.Contains("powerpoint") || ct.Contains("presentation") => "PPT演示",
            var ct when ct.Contains("text") => "文本文件",
            var ct when ct.Contains("video") => "视频文件",
            var ct when ct.Contains("audio") => "音频文件",
            var ct when ct.Contains("zip") || ct.Contains("compressed") => "压缩文件",
            _ => "其他文件"
        };
    }
} 