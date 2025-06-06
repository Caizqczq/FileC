using CloudFileHub.Data;
using CloudFileHub.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudFileHub.Services;

public class FileService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly AliyunOSSService _ossService;
    private readonly DocumentTextExtractorService _textExtractor;
    private readonly IAiAssistantService _aiAssistant;
    private readonly ILogger<FileService> _logger;

    public FileService(
        ApplicationDbContext context, 
        IWebHostEnvironment environment, 
        AliyunOSSService ossService,
        DocumentTextExtractorService textExtractor,
        IAiAssistantService aiAssistant,
        ILogger<FileService> logger)
    {
        _context = context;
        _environment = environment;
        _ossService = ossService;
        _textExtractor = textExtractor;
        _aiAssistant = aiAssistant;
        _logger = logger;
    }

    public async Task<List<FileModel>> GetUserFilesAsync(string userId, int? directoryId = null)
    {
        return await _context.Files
            .Where(f => f.UserId == userId && f.DirectoryId == directoryId)
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户的所有文件（包括所有子目录中的文件）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户的所有文件列表</returns>
    public async Task<List<FileModel>> GetAllUserFilesAsync(string userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId)
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
        try
        {
            // 基本验证
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("文件不能为空");
            }

            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                throw new ArgumentException("文件名不能为空");
            }

            // 检查文件扩展名
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                throw new ArgumentException("文件必须有扩展名");
            }

            _logger.LogInformation("开始上传文件: {FileName}, 大小: {Size}, 类型: {ContentType}", 
                file.FileName, file.Length, file.ContentType);

            // 检查同一目录下是否已存在同名文件
            string originalFileName = Path.GetFileName(file.FileName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
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

            _logger.LogInformation("文件上传成功: {FileName}, 数据库ID: {FileId}", finalFileName, fileModel.Id);
            return fileModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败: {FileName}, 用户ID: {UserId}", file?.FileName, userId);
            throw;
        }
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



    /// <summary>
    /// 对文件进行AI分析
    /// </summary>
    public async Task AnalyzeFileWithAiAsync(int fileId)
    {
        try
        {
            var fileModel = await _context.Files.FindAsync(fileId);
            if (fileModel == null) return;

            // 检查是否支持AI分析
            if (!_textExtractor.IsSupportedFile(fileModel.ContentType, fileModel.FileName))
            {
                _logger.LogInformation("文件类型不支持AI分析: {FileName}", fileModel.FileName);
                return;
            }

            // 从OSS下载文件内容
            var fileStream = await _ossService.DownloadFileAsync(fileModel.FilePath);
            if (fileStream == null)
            {
                _logger.LogWarning("无法从OSS下载文件: {FileName}", fileModel.FileName);
                return;
            }

            string extractedText;
            using (fileStream)
            {
                extractedText = await _textExtractor.ExtractTextAsync(fileStream, fileModel.FileName, fileModel.ContentType);
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("无法从文件中提取文本内容: {FileName}", fileModel.FileName);
                return;
            }

            // 清理文本
            var cleanText = _textExtractor.CleanExtractedText(extractedText);

            // AI分析
            var analysisResult = await _aiAssistant.AnalyzeDocumentAsync(cleanText, fileModel.FileName, fileModel.ContentType);

            // 更新文件模型
            fileModel.AiSummary = analysisResult.Summary;
            fileModel.AiCategory = analysisResult.Category;
            fileModel.AiTags = string.Join(", ", analysisResult.Tags);
            fileModel.LastAiAnalysis = DateTime.UtcNow;

            // 删除旧的分析结果（如果存在）
            var oldResult = await _context.AiAnalysisResults.FirstOrDefaultAsync(a => a.FileId == fileId);
            if (oldResult != null)
            {
                _context.AiAnalysisResults.Remove(oldResult);
            }

            // 保存新的AI分析结果到详细表
            var aiResult = new AiAnalysisResult
            {
                FileId = fileId,
                Summary = analysisResult.Summary,
                Category = analysisResult.Category,
                Tags = System.Text.Json.JsonSerializer.Serialize(analysisResult.Tags),
                Confidence = analysisResult.Confidence,
                Language = analysisResult.Language,
                ExtractedContent = cleanText.Length > 5000 ? cleanText.Substring(0, 5000) : cleanText
            };

            _context.AiAnalysisResults.Add(aiResult);
            await _context.SaveChangesAsync();

            _logger.LogInformation("文件AI分析完成: {FileName}, 类别: {Category}", fileModel.FileName, analysisResult.Category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件AI分析失败: FileId={FileId}", fileId);
        }
    }

    /// <summary>
    /// 对已存在的文件进行AI重新分析
    /// </summary>
    public async Task<bool> ReanalyzeFileAsync(int fileId, string userId)
    {
        try
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
            if (file == null)
            {
                return false;
            }

            // 检查是否支持AI分析
            if (!_textExtractor.IsSupportedFile(file.ContentType, file.FileName))
            {
                return false;
            }

            // 下载文件内容
            var fileStream = await _ossService.DownloadFileAsync(file.FilePath);
            if (fileStream == null)
            {
                return false;
            }

            using (fileStream)
            {
                // 提取文档内容
                var extractedText = await _textExtractor.ExtractTextAsync(fileStream, file.FileName, file.ContentType);
                
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return false;
                }

                // 清理文本
                var cleanText = _textExtractor.CleanExtractedText(extractedText);

                // AI分析
                var analysisResult = await _aiAssistant.AnalyzeDocumentAsync(cleanText, file.FileName, file.ContentType);

                // 更新文件模型
                file.AiSummary = analysisResult.Summary;
                file.AiCategory = analysisResult.Category;
                file.AiTags = string.Join(", ", analysisResult.Tags);
                file.LastAiAnalysis = DateTime.UtcNow;

                // 删除旧的分析结果
                var oldResult = await _context.AiAnalysisResults.FirstOrDefaultAsync(a => a.FileId == fileId);
                if (oldResult != null)
                {
                    _context.AiAnalysisResults.Remove(oldResult);
                }

                // 保存新的AI分析结果
                var aiResult = new AiAnalysisResult
                {
                    FileId = fileId,
                    Summary = analysisResult.Summary,
                    Category = analysisResult.Category,
                    Tags = System.Text.Json.JsonSerializer.Serialize(analysisResult.Tags),
                    Confidence = analysisResult.Confidence,
                    Language = analysisResult.Language,
                    ExtractedContent = cleanText.Length > 5000 ? cleanText.Substring(0, 5000) : cleanText
                };

                _context.AiAnalysisResults.Add(aiResult);
                await _context.SaveChangesAsync();

                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件AI重新分析失败: FileId={FileId}", fileId);
            return false;
        }
    }

    /// <summary>
    /// 获取文件的AI分析结果
    /// </summary>
    public async Task<AiAnalysisResult?> GetFileAnalysisAsync(int fileId, string userId)
    {
        return await _context.AiAnalysisResults
            .Include(a => a.File)
            .FirstOrDefaultAsync(a => a.FileId == fileId && a.File!.UserId == userId);
    }

    /// <summary>
    /// 根据AI类别搜索文件
    /// </summary>
    public async Task<List<FileModel>> SearchFilesByCategoryAsync(string category, string userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId && f.AiCategory == category)
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();
    }

    /// <summary>
    /// 根据AI标签搜索文件
    /// </summary>
    public async Task<List<FileModel>> SearchFilesByTagAsync(string tag, string userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId && f.AiTags != null && f.AiTags.Contains(tag))
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();
    }
}
