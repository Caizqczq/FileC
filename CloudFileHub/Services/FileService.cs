using CloudFileHub.Data;
using CloudFileHub.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudFileHub.Services;

public class FileService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly AliyunOSSService _ossService;

    public FileService(ApplicationDbContext context, IWebHostEnvironment environment, AliyunOSSService ossService)
    {
        _context = context;
        _environment = environment;
        _ossService = ossService;
    }

    public async Task<List<FileModel>> GetUserFilesAsync(string userId, int? directoryId = null)
    {
        return await _context.Files
            .Where(f => f.UserId == userId && f.DirectoryId == directoryId)
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();
    }

    public async Task<List<DirectoryModel>> GetUserDirectoriesAsync(string userId, int? parentDirectoryId = null)
    {
        return await _context.Directories
            .Where(d => d.UserId == userId && d.ParentId == parentDirectoryId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<FileModel?> GetFileByIdAsync(int fileId, string userId)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && (f.UserId == userId || f.IsPublic));
    }

    public async Task<DirectoryModel?> GetDirectoryByIdAsync(int directoryId, string userId)
    {
        return await _context.Directories
            .FirstOrDefaultAsync(d => d.Id == directoryId && (d.UserId == userId || d.IsPublic));
    }

    public async Task<FileModel> UploadFileAsync(IFormFile file, string userId, int? directoryId = null, string? description = null)
    {
        // 检查同一目录下是否已存在同名文件
        string originalFileName = Path.GetFileName(file.FileName);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        string extension = Path.GetExtension(originalFileName);
        string finalFileName = originalFileName;

        // 查询数据库中是否存在同名文件
        bool fileExists = await _context.Files
            .AnyAsync(f => f.UserId == userId && f.DirectoryId == directoryId && f.FileName == originalFileName);

        // 如果存在同名文件，添加计数后缀
        if (fileExists)
        {
            int counter = 1;
            string newFileName;

            do
            {
                newFileName = $"{fileNameWithoutExtension} ({counter}){extension}";
                counter++;
                fileExists = await _context.Files
                    .AnyAsync(f => f.UserId == userId && f.DirectoryId == directoryId && f.FileName == newFileName);
            } while (fileExists);

            finalFileName = newFileName;
        }

        // Generate unique filename for OSS storage (生成OSS存储的唯一键名)
        var uniqueFileName = $"{userId}/{Guid.NewGuid()}_{finalFileName}";

        // Upload file to OSS
        using (var stream = file.OpenReadStream())
        {
            var uploadSuccess = await _ossService.UploadFileAsync(uniqueFileName, stream);
            if (!uploadSuccess)
            {
                throw new Exception("文件上传到OSS失败");
            }
        }

        // Create file record in database
        var fileModel = new FileModel
        {
            FileName = finalFileName, // 使用可能已修改的文件名
            ContentType = file.ContentType,
            FileSize = file.Length,
            FilePath = uniqueFileName, // 存储OSS的键名
            Description = description,
            UserId = userId,
            DirectoryId = directoryId
        };

        _context.Files.Add(fileModel);
        await _context.SaveChangesAsync();

        // Update user storage usage
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.StorageUsed += file.Length;
            await _context.SaveChangesAsync();
        }

        return fileModel;
    }

    public async Task<bool> DeleteFileAsync(int fileId, string userId)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
        if (file == null)
        {
            return false;
        }

        // Delete file from OSS
        await _ossService.DeleteFileAsync(file.FilePath);

        // Update user storage usage
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.StorageUsed -= file.FileSize;
            await _context.SaveChangesAsync();
        }

        // Remove from database
        _context.Files.Remove(file);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 获取文件的下载流
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>文件流</returns>
    public async Task<(Stream? Stream, string? FileName, string? ContentType)> GetFileStreamAsync(int fileId, string userId)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && (f.UserId == userId || f.IsPublic));
        
        if (file == null)
        {
            return (null, null, null);
        }

        var stream = await _ossService.DownloadFileAsync(file.FilePath);
        return (stream, file.FileName, file.ContentType);
    }

    /// <summary>
    /// 生成文件的预签名下载URL
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>预签名URL</returns>
    public async Task<string?> GenerateDownloadUrlAsync(int fileId, string userId, TimeSpan? expiration = null)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && (f.UserId == userId || f.IsPublic));
        
        if (file == null)
        {
            return null;
        }

        return _ossService.GeneratePresignedUrl(file.FilePath, expiration);
    }

    public async Task<DirectoryModel> CreateDirectoryAsync(string name, string userId, int? parentDirectoryId = null)
    {
        var directory = new DirectoryModel
        {
            Name = name,
            UserId = userId,
            ParentId = parentDirectoryId
        };

        _context.Directories.Add(directory);
        await _context.SaveChangesAsync();

        return directory;
    }

    public async Task<bool> DeleteDirectoryAsync(int directoryId, string userId)
    {
        var directory = await _context.Directories
            .Include(d => d.Files)
            .Include(d => d.Subdirectories)
            .FirstOrDefaultAsync(d => d.Id == directoryId && d.UserId == userId);

        if (directory == null)
        {
            return false;
        }

        // Recursively delete subdirectories
        foreach (var subdir in directory.Subdirectories.ToList())
        {
            await DeleteDirectoryAsync(subdir.Id, userId);
        }

        // Delete all files in this directory
        foreach (var file in directory.Files.ToList())
        {
            await DeleteFileAsync(file.Id, userId);
        }

        // Remove directory from database
        _context.Directories.Remove(directory);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RenameFileAsync(int fileId, string newName, string userId)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
        if (file == null)
        {
            return false;
        }

        file.FileName = newName;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RenameDirectoryAsync(int directoryId, string newName, string userId)
    {
        var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Id == directoryId && d.UserId == userId);
        if (directory == null)
        {
            return false;
        }

        directory.Name = newName;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MoveFileAsync(int fileId, int? targetDirectoryId, string userId)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
        if (file == null)
        {
            return false;
        }

        // If targetDirectoryId is not null, verify it exists and belongs to the user
        if (targetDirectoryId.HasValue)
        {
            var targetDirectory = await _context.Directories.FirstOrDefaultAsync(d => d.Id == targetDirectoryId && d.UserId == userId);
            if (targetDirectory == null)
            {
                return false;
            }
        }

        file.DirectoryId = targetDirectoryId;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MoveDirectoryAsync(int directoryId, int? targetParentId, string userId)
    {
        var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Id == directoryId && d.UserId == userId);
        if (directory == null)
        {
            return false;
        }

        // Prevent moving a directory into itself or its subdirectories
        if (targetParentId.HasValue)
        {
            var targetParent = await _context.Directories.FirstOrDefaultAsync(d => d.Id == targetParentId && d.UserId == userId);
            if (targetParent == null)
            {
                return false;
            }

            // Check if target is the directory itself or a subdirectory
            var currentParentId = targetParent.ParentId;
            while (currentParentId.HasValue)
            {
                if (currentParentId.Value == directoryId)
                {
                    return false; // Would create a cycle
                }

                var currentParent = await _context.Directories.FirstOrDefaultAsync(d => d.Id == currentParentId);
                if (currentParent == null)
                {
                    break;
                }

                currentParentId = currentParent.ParentId;
            }
        }

        directory.ParentId = targetParentId;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(int SuccessCount, int FailCount)> BatchDeleteFilesAsync(List<int> fileIds, string userId)
    {
        int successCount = 0;
        int failCount = 0;

        foreach (var fileId in fileIds)
        {
            bool result = await DeleteFileAsync(fileId, userId);
            if (result)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        return (successCount, failCount);
    }

    public async Task<(int SuccessCount, int FailCount)> BatchDeleteDirectoriesAsync(List<int> directoryIds, string userId)
    {
        int successCount = 0;
        int failCount = 0;

        foreach (var directoryId in directoryIds)
        {
            bool result = await DeleteDirectoryAsync(directoryId, userId);
            if (result)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        return (successCount, failCount);
    }

    public async Task<(int SuccessCount, int FailCount)> BatchMoveFilesAsync(List<int> fileIds, int? targetDirectoryId, string userId)
    {
        int successCount = 0;
        int failCount = 0;

        // Verify target directory exists and belongs to user
        if (targetDirectoryId.HasValue)
        {
            var targetDirectory = await _context.Directories.FirstOrDefaultAsync(d => d.Id == targetDirectoryId && d.UserId == userId);
            if (targetDirectory == null)
            {
                return (0, fileIds.Count);
            }
        }

        foreach (var fileId in fileIds)
        {
            bool result = await MoveFileAsync(fileId, targetDirectoryId, userId);
            if (result)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        return (successCount, failCount);
    }

    public async Task<(int SuccessCount, int FailCount)> BatchMoveDirectoriesAsync(List<int> directoryIds, int? targetParentId, string userId)
    {
        int successCount = 0;
        int failCount = 0;

        // Verify target directory exists and belongs to user
        if (targetParentId.HasValue)
        {
            var targetDirectory = await _context.Directories.FirstOrDefaultAsync(d => d.Id == targetParentId && d.UserId == userId);
            if (targetDirectory == null)
            {
                return (0, directoryIds.Count);
            }
        }

        foreach (var directoryId in directoryIds)
        {
            // Skip if trying to move a directory to itself
            if (targetParentId.HasValue && targetParentId.Value == directoryId)
            {
                failCount++;
                continue;
            }

            bool result = await MoveDirectoryAsync(directoryId, targetParentId, userId);
            if (result)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        return (successCount, failCount);
    }

    public async Task<List<DirectoryModel>> SearchDirectoriesAsync(string searchTerm, string userId)
    {
        return await _context.Directories
            .Where(d => d.UserId == userId && d.Name.Contains(searchTerm))
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<FileModel>> SearchFilesAsync(string searchTerm, string userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId &&
                   (f.FileName.Contains(searchTerm) ||
                    (f.Description != null && f.Description.Contains(searchTerm))))
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();
    }
}
