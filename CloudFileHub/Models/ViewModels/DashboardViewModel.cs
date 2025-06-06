namespace CloudFileHub.Models.ViewModels;

public class DashboardViewModel
{
    public UserStatsViewModel UserStats { get; set; } = new();
    public List<FileModel> RecentFiles { get; set; } = new();
    public List<FileModel> RecentAiAnalyzedFiles { get; set; } = new();
    public List<FileShareModel> RecentShares { get; set; } = new();
    public StorageStatsViewModel StorageStats { get; set; } = new();
    public ActivityStatsViewModel ActivityStats { get; set; } = new();
}

public class UserStatsViewModel
{
    public int TotalFiles { get; set; }
    public int TotalDirectories { get; set; }
    public long TotalStorageUsed { get; set; }
    public long StorageLimit { get; set; }
    public int TotalShares { get; set; }
    public int FilesWithAiAnalysis { get; set; }
    public double StorageUsagePercentage => StorageLimit > 0 ? (double)TotalStorageUsed / StorageLimit * 100 : 0;
    public string FormattedStorageUsed => FormatFileSize(TotalStorageUsed);
    public string FormattedStorageLimit => FormatFileSize(StorageLimit);
    
    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}

public class StorageStatsViewModel
{
    public Dictionary<string, long> FileTypeStorage { get; set; } = new();
    public Dictionary<string, int> FileTypeCount { get; set; } = new();
}

public class ActivityStatsViewModel
{
    public int FilesUploadedToday { get; set; }
    public int FilesUploadedThisWeek { get; set; }
    public int FilesUploadedThisMonth { get; set; }
    public int SharesCreatedThisWeek { get; set; }
    public int AiAnalysisThisWeek { get; set; }
    public DateTime? LastLoginDate { get; set; }
} 