using CloudFileHub.Models;
using CloudFileHub.Models.ViewModels;
using CloudFileHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace CloudFileHub.Controllers;

[Authorize]
public class FileController : Controller
{
    private readonly FileService _fileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public FileController(
        FileService fileService,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _fileService = fileService;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Index(int? directoryId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var files = await _fileService.GetUserFilesAsync(userId, directoryId);
        var directories = await _fileService.GetUserDirectoriesAsync(userId, directoryId);

        // Get current directory info for breadcrumb navigation
        DirectoryModel? currentDirectory = null;
        if (directoryId.HasValue)
        {
            currentDirectory = await _fileService.GetDirectoryByIdAsync(directoryId.Value, userId);
        }

        ViewBag.CurrentDirectory = currentDirectory;
        ViewBag.DirectoryId = directoryId;

        var viewModel = new FileExplorerViewModel
        {
            Files = files,
            Directories = directories,
            CurrentDirectoryId = directoryId
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Upload(int? directoryId = null)
    {
        ViewBag.DirectoryId = directoryId;

        // 获取当前目录信息
        if (directoryId.HasValue)
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                var currentDirectory = await _fileService.GetDirectoryByIdAsync(directoryId.Value, userId);
                ViewBag.CurrentDirectory = currentDirectory;
            }
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, int? directoryId = null, string? description = null, bool? isPublic = null)
    {
        // 单文件上传（向后兼容）
        if (file != null)
        {
            return await UploadSingleFile(file, directoryId, description, isPublic);
        }
        
        ModelState.AddModelError("", "请选择要上传的文件。");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadMultiple(List<IFormFile> files, int? directoryId = null, string? description = null, bool? isPublic = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        // 设置当前目录信息
        ViewBag.DirectoryId = directoryId;
        if (directoryId.HasValue)
        {
            var currentDirectory = await _fileService.GetDirectoryByIdAsync(directoryId.Value, userId);
            ViewBag.CurrentDirectory = currentDirectory;
        }

        if (files == null || files.Count == 0)
        {
            ModelState.AddModelError("", "请选择要上传的文件。");
            return View("Upload");
        }

        var uploadResults = new List<(string FileName, bool Success, string? Error)>();
        var logger = HttpContext.RequestServices.GetService<ILogger<FileController>>();
        
        // 检查用户存储限制
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var totalSize = files.Sum(f => f.Length);
            if (user.StorageUsed + totalSize > user.StorageLimit)
            {
                ModelState.AddModelError("", "所选文件总大小超出存储限制。");
                return View("Upload");
            }
        }

        foreach (var file in files)
        {
            try
            {
                var result = await ValidateAndUploadSingleFile(file, userId, directoryId, description, logger);
                uploadResults.Add((file.FileName, result.Success, result.Error));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "上传文件 {FileName} 时发生错误", file.FileName);
                uploadResults.Add((file.FileName, false, ex.Message));
            }
        }

        // 生成结果消息
        var successCount = uploadResults.Count(r => r.Success);
        var failCount = uploadResults.Count(r => !r.Success);

        if (successCount > 0)
        {
            TempData["SuccessMessage"] = $"成功上传 {successCount} 个文件";
            if (failCount > 0)
            {
                TempData["WarningMessage"] = $"有 {failCount} 个文件上传失败";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "所有文件上传失败";
        }

        return RedirectToAction(nameof(Index), new { directoryId });
    }

    private async Task<IActionResult> UploadSingleFile(IFormFile file, int? directoryId = null, string? description = null, bool? isPublic = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        // 设置当前目录信息，无论是否验证成功
        ViewBag.DirectoryId = directoryId;
        if (directoryId.HasValue)
        {
            var currentDirectory = await _fileService.GetDirectoryByIdAsync(directoryId.Value, userId);
            ViewBag.CurrentDirectory = currentDirectory;
        }

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "请选择要上传的文件。");
            return View();
        }

        // 记录详细的文件信息用于调试
        var fileLogger = HttpContext.RequestServices.GetService<ILogger<FileController>>();
        fileLogger?.LogInformation("接收到文件上传请求: 文件名={FileName}, 大小={Size}, ContentType={ContentType}", 
            file.FileName, file.Length, file.ContentType);

        // 检查文件大小
        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        if (file.Length > maxFileSize)
        {
            ModelState.AddModelError("", $"文件大小不能超过 {maxFileSize / (1024 * 1024)} MB。");
            return View();
        }

        // 检查文件类型安全性
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js" };
        if (dangerousExtensions.Contains(fileExtension))
        {
            fileLogger?.LogWarning("文件扩展名被拒绝: {FileName}, 扩展名: {Extension}", file.FileName, fileExtension);
            ModelState.AddModelError("", "出于安全考虑，不允许上传此类型的文件。");
            return View();
        }

        // 验证Content-Type - 阻止危险的可执行文件类型
        if (string.IsNullOrEmpty(file.ContentType))
        {
            fileLogger?.LogWarning("文件ContentType为空: {FileName}", file.FileName);
            ModelState.AddModelError("", "无法检测文件类型，上传被拒绝。");
            return View();
        }

        // 明确阻止的危险Content-Type
        var dangerousContentTypes = new[] { 
            "application/x-msdownload", 
            "application/octet-stream",
            "application/x-executable",
            "application/x-dosexec"
        };
        
        // 检查文件是否为可执行文件（通过扩展名和Content-Type双重验证）
        if (dangerousContentTypes.Any(ct => file.ContentType.Contains(ct)) && 
            dangerousExtensions.Contains(fileExtension))
        {
            fileLogger?.LogWarning("危险文件类型被拒绝: {FileName}, ContentType: {ContentType}, 扩展名: {Extension}", 
                file.FileName, file.ContentType, fileExtension);
            ModelState.AddModelError("", "出于安全考虑，不允许上传可执行文件。");
            return View();
        }

        // Check if user has enough storage space
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && user.StorageUsed + file.Length > user.StorageLimit)
        {
            ModelState.AddModelError("", "您已超出存储限制。");
            return View();
        }

        try
        {
            fileLogger?.LogInformation("开始处理文件上传: {FileName}", file.FileName);
            var uploadedFile = await _fileService.UploadFileAsync(file, userId, directoryId, description);
            
            fileLogger?.LogInformation("文件上传成功: {FileName}, 文件ID: {FileId}", file.FileName, uploadedFile.Id);
            
            // 添加成功消息
            TempData["SuccessMessage"] = "文件上传成功！如需AI分析，请点击文件的'AI分析'按钮。";
            
            return RedirectToAction(nameof(Index), new { directoryId });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", $"文件处理失败: {ex.Message}");
            return View();
        }
        catch (Exception ex)
        {
            // 记录详细错误日志
            var logger = HttpContext.RequestServices.GetService<ILogger<FileController>>();
            logger?.LogError(ex, "文件上传失败: FileName={FileName}, ContentType={ContentType}, Size={Size}", 
                file.FileName, file.ContentType, file.Length);
            
            ModelState.AddModelError("", "文件上传失败，请稍后重试。如果问题持续存在，请联系管理员。");
            return View();
        }
    }

    private async Task<(bool Success, string? Error)> ValidateAndUploadSingleFile(IFormFile file, string userId, int? directoryId, string? description, ILogger<FileController>? logger)
    {
        // 基本验证
        if (file == null || file.Length == 0)
        {
            return (false, "文件为空");
        }

        // 检查文件大小
        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        if (file.Length > maxFileSize)
        {
            return (false, $"文件大小不能超过 {maxFileSize / (1024 * 1024)} MB");
        }

        // 检查文件类型安全性
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js" };
        if (dangerousExtensions.Contains(fileExtension))
        {
            logger?.LogWarning("文件扩展名被拒绝: {FileName}, 扩展名: {Extension}", file.FileName, fileExtension);
            return (false, "出于安全考虑，不允许上传此类型的文件");
        }

        // 验证Content-Type
        if (string.IsNullOrEmpty(file.ContentType))
        {
            logger?.LogWarning("文件ContentType为空: {FileName}", file.FileName);
            return (false, "无法检测文件类型，上传被拒绝");
        }

        // 明确阻止的危险Content-Type
        var dangerousContentTypes = new[] { 
            "application/x-msdownload", 
            "application/octet-stream",
            "application/x-executable",
            "application/x-dosexec"
        };
        
        if (dangerousContentTypes.Any(ct => file.ContentType.Contains(ct)) && 
            dangerousExtensions.Contains(fileExtension))
        {
            logger?.LogWarning("危险文件类型被拒绝: {FileName}, ContentType: {ContentType}, 扩展名: {Extension}", 
                file.FileName, file.ContentType, fileExtension);
            return (false, "出于安全考虑，不允许上传可执行文件");
        }

        try
        {
            logger?.LogInformation("开始处理文件上传: {FileName}", file.FileName);
            var uploadedFile = await _fileService.UploadFileAsync(file, userId, directoryId, description);
            logger?.LogInformation("文件上传成功: {FileName}, 文件ID: {FileId}", file.FileName, uploadedFile.Id);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "文件上传失败: {FileName}", file.FileName);
            return (false, ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var (stream, fileName, contentType) = await _fileService.GetFileStreamAsync(id, userId);
        if (stream == null || fileName == null || contentType == null)
        {
            return NotFound();
        }

        return File(stream, contentType, fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? directoryId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var result = await _fileService.DeleteFileAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { directoryId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateDirectory(int? parentDirectoryId = null)
    {
        ViewBag.ParentDirectoryId = parentDirectoryId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDirectory(string name, int? parentDirectoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("", "Directory name is required.");
            ViewBag.ParentDirectoryId = parentDirectoryId;
            return View();
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        await _fileService.CreateDirectoryAsync(name, userId, parentDirectoryId);
        return RedirectToAction(nameof(Index), new { directoryId = parentDirectoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDirectory(int id, int? parentDirectoryId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var result = await _fileService.DeleteDirectoryAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { directoryId = parentDirectoryId });
    }

    [HttpGet]
    public async Task<IActionResult> RenameFile(int id, int? directoryId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var file = await _fileService.GetFileByIdAsync(id, userId);
        if (file == null)
        {
            return NotFound();
        }

        ViewBag.DirectoryId = directoryId;
        return View(file);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameFile(int id, string newName, int? directoryId = null)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            ModelState.AddModelError("", "File name is required.");
            return View();
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var result = await _fileService.RenameFileAsync(id, newName, userId);
        if (!result)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { directoryId });
    }

    [HttpGet]
    public async Task<IActionResult> RenameDirectory(int id, int? parentDirectoryId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var directory = await _fileService.GetDirectoryByIdAsync(id, userId);
        if (directory == null)
        {
            return NotFound();
        }

        ViewBag.ParentDirectoryId = parentDirectoryId;
        return View(directory);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameDirectory(int id, string newName, int? parentDirectoryId = null)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            ModelState.AddModelError("", "Directory name is required.");
            return View();
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var result = await _fileService.RenameDirectoryAsync(id, newName, userId);
        if (!result)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { directoryId = parentDirectoryId });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string query, int? directoryId = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return RedirectToAction(nameof(Index), new { directoryId });
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var files = await _fileService.SearchFilesAsync(query, userId);
        var directories = await _fileService.SearchDirectoriesAsync(query, userId);

        var viewModel = new FileExplorerViewModel
        {
            Files = files,
            Directories = directories,
            CurrentDirectoryId = directoryId
        };

        ViewBag.SearchQuery = query;
        ViewBag.DirectoryId = directoryId;

        return View("SearchResults", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> SelectDestination(int? currentDirectoryId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var directories = await _fileService.GetUserDirectoriesAsync(userId, null);

        ViewBag.CurrentDirectoryId = currentDirectoryId;
        return View(directories);
    }

    private async Task<IActionResult> BatchDownloadFiles(List<int> fileIds, string userId)
    {
        if (fileIds == null || fileIds.Count == 0)
        {
            return BadRequest("未选择任何文件");
        }

        // 获取所有选中的文件
        var files = new List<FileModel>();
        foreach (var fileId in fileIds)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            if (file != null)
            {
                files.Add(file);
            }
        }

        if (files.Count == 0)
        {
            return NotFound("未找到任何选中的文件");
        }

        // 如果只有一个文件，直接下载
        if (files.Count == 1)
        {
            var file = files[0];
            var (stream, fileName, contentType) = await _fileService.GetFileStreamAsync(file.Id, userId);
            if (stream == null || fileName == null || contentType == null)
            {
                return NotFound("文件不存在");
            }

            return File(stream, contentType, fileName);
        }

        // 多个文件，创建ZIP压缩包
        var zipFileName = $"批量下载_{DateTime.Now:yyyyMMddHHmmss}.zip";
        var tempZipPath = Path.Combine(Path.GetTempPath(), zipFileName);

        try
        {
            using (var zipArchive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var (stream, fileName, contentType) = await _fileService.GetFileStreamAsync(file.Id, userId);
                    if (stream != null && fileName != null)
                    {
                        // 添加文件到ZIP，使用原始文件名
                        var entry = zipArchive.CreateEntry(fileName);
                        using (var entryStream = entry.Open())
                        {
                            await stream.CopyToAsync(entryStream);
                        }
                        stream.Dispose();
                    }
                }
            }

            // 读取ZIP文件并返回
            var zipBytes = await System.IO.File.ReadAllBytesAsync(tempZipPath);

            // 删除临时ZIP文件
            System.IO.File.Delete(tempZipPath);

            return File(zipBytes, "application/zip", zipFileName);
        }
        catch (Exception ex)
        {
            // 确保临时文件被删除
            if (System.IO.File.Exists(tempZipPath))
            {
                System.IO.File.Delete(tempZipPath);
            }

            return StatusCode(500, $"创建ZIP文件时出错: {ex.Message}");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchOperation(BatchOperationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index), new { directoryId = model.CurrentDirectoryId });
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        // 处理批量下载操作
        if (model.Operation == "download" && model.FileIds.Count > 0)
        {
            return await BatchDownloadFiles(model.FileIds, userId);
        }

        if (model.Operation == "delete")
        {
            // Delete files
            if (model.FileIds.Any())
            {
                var fileResult = await _fileService.BatchDeleteFilesAsync(model.FileIds, userId);
                TempData["SuccessMessage"] = $"成功删除 {fileResult.SuccessCount} 个文件";

                if (fileResult.FailCount > 0)
                {
                    TempData["ErrorMessage"] = $"无法删除 {fileResult.FailCount} 个文件";
                }
            }

            // Delete directories
            if (model.DirectoryIds.Any())
            {
                var dirResult = await _fileService.BatchDeleteDirectoriesAsync(model.DirectoryIds, userId);

                if (TempData.ContainsKey("SuccessMessage"))
                {
                    TempData["SuccessMessage"] = $"{TempData["SuccessMessage"]}，成功删除 {dirResult.SuccessCount} 个文件夹";
                }
                else
                {
                    TempData["SuccessMessage"] = $"成功删除 {dirResult.SuccessCount} 个文件夹";
                }

                if (dirResult.FailCount > 0)
                {
                    if (TempData.ContainsKey("ErrorMessage"))
                    {
                        TempData["ErrorMessage"] = $"{TempData["ErrorMessage"]}，无法删除 {dirResult.FailCount} 个文件夹";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"无法删除 {dirResult.FailCount} 个文件夹";
                    }
                }
            }
        }
        else if (model.Operation == "move")
        {
            // 检查是否选择了目标文件夹
            if (!model.HasSelectedTarget)
            {
                TempData["ErrorMessage"] = "请选择目标文件夹";
                return RedirectToAction(nameof(Index), new { directoryId = model.CurrentDirectoryId });
            }

            // Move files
            if (model.FileIds.Any())
            {
                var fileResult = await _fileService.BatchMoveFilesAsync(model.FileIds, model.TargetDirectoryId, userId);
                TempData["SuccessMessage"] = $"成功移动 {fileResult.SuccessCount} 个文件";

                if (fileResult.FailCount > 0)
                {
                    TempData["ErrorMessage"] = $"无法移动 {fileResult.FailCount} 个文件";
                }
            }

            // Move directories
            if (model.DirectoryIds.Any())
            {
                var dirResult = await _fileService.BatchMoveDirectoriesAsync(model.DirectoryIds, model.TargetDirectoryId, userId);

                if (TempData.ContainsKey("SuccessMessage"))
                {
                    TempData["SuccessMessage"] = $"{TempData["SuccessMessage"]}，成功移动 {dirResult.SuccessCount} 个文件夹";
                }
                else
                {
                    TempData["SuccessMessage"] = $"成功移动 {dirResult.SuccessCount} 个文件夹";
                }

                if (dirResult.FailCount > 0)
                {
                    if (TempData.ContainsKey("ErrorMessage"))
                    {
                        TempData["ErrorMessage"] = $"{TempData["ErrorMessage"]}，无法移动 {dirResult.FailCount} 个文件夹";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"无法移动 {dirResult.FailCount} 个文件夹";
                    }
                }
            }
        }

        return RedirectToAction(nameof(Index), new { directoryId = model.CurrentDirectoryId });
    }

    /// <summary>
    /// AI分析仪表板
    /// </summary>
    public IActionResult AiDashboard()
    {
        return View();
    }

    /// <summary>
    /// 查看文件详情
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var file = await _fileService.GetFileByIdAsync(id, userId);
        if (file == null)
        {
            return NotFound();
        }

        // 获取AI分析结果
        var aiAnalysis = await _fileService.GetFileAnalysisAsync(id, userId);

        // 生成预览URL（对于图片、PDF等可预览的文件）
        string? previewUrl = null;
        bool canPreview = false;

        if (file.ContentType.StartsWith("image/"))
        {
            previewUrl = await _fileService.GenerateDownloadUrlAsync(id, userId, TimeSpan.FromHours(1));
            canPreview = true;
        }
        else if (file.ContentType.Contains("pdf"))
        {
            previewUrl = await _fileService.GenerateDownloadUrlAsync(id, userId, TimeSpan.FromHours(1));
            canPreview = true;
        }

        // 获取相关文件（同类别的其他文件）
        var relatedFiles = new List<FileModel>();
        if (!string.IsNullOrEmpty(file.AiCategory))
        {
            var categoryFiles = await _fileService.SearchFilesByCategoryAsync(file.AiCategory, userId);
            relatedFiles = categoryFiles.Where(f => f.Id != file.Id).Take(5).ToList();
        }

        var viewModel = new FileDetailViewModel
        {
            File = file,
            AiAnalysis = aiAnalysis,
            PreviewUrl = previewUrl,
            CanPreview = canPreview,
            ExtractedContent = aiAnalysis?.ExtractedContent,
            RelatedFiles = relatedFiles
        };

        return View(viewModel);
    }

    /// <summary>
    /// 获取文件预览内容（用于AJAX请求）
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Preview(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var file = await _fileService.GetFileByIdAsync(id, userId);
        if (file == null)
        {
            return NotFound();
        }

        // 只允许预览图片和PDF文件
        if (file.ContentType.StartsWith("image/") || file.ContentType.Contains("pdf"))
        {
            var (stream, fileName, contentType) = await _fileService.GetFileStreamAsync(id, userId);
            if (stream != null && contentType != null)
            {
                // 设置适当的缓存头
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["ETag"] = $"\"{file.Id}-{file.UploadDate.Ticks}\"";
                
                return File(stream, contentType, enableRangeProcessing: true);
            }
        }

        return NotFound();
    }

    /// <summary>
    /// 获取支持的文件格式信息
    /// </summary>
    [HttpGet]
    public IActionResult GetSupportedFormats()
    {
        var supportedFormats = new
        {
            uploadFormats = new[]
            {
                "所有格式文件都可以上传"
            },
            aiAnalysisFormats = new[]
            {
                new { type = "PDF文档", extensions = new[] { ".pdf" }, description = "PDF格式文档" },
                new { type = "Word文档", extensions = new[] { ".doc", ".docx" }, description = "Microsoft Word文档" },
                new { type = "文本文件", extensions = new[] { ".txt" }, description = "纯文本文件" },
                new { type = "CSV文件", extensions = new[] { ".csv" }, description = "逗号分隔值文件" },
                new { type = "RTF文件", extensions = new[] { ".rtf" }, description = "富文本格式文件" }
            },
            maxFileSize = "100MB",
            note = "只有支持AI分析的文件格式才能进行智能分析和标签提取"
        };

        return Ok(supportedFormats);
    }


}
