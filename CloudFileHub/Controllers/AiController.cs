using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CloudFileHub.Services;
using CloudFileHub.Models;
using System.Security.Claims;

namespace CloudFileHub.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AiController : ControllerBase
{
    private readonly FileService _fileService;
    private readonly IAiAssistantService _aiAssistant;
    private readonly ILogger<AiController> _logger;

    public AiController(
        FileService fileService,
        IAiAssistantService aiAssistant,
        ILogger<AiController> logger)
    {
        _fileService = fileService;
        _aiAssistant = aiAssistant;
        _logger = logger;
    }

    /// <summary>
    /// 重新分析文件
    /// </summary>
    [HttpPost("reanalyze/{fileId}")]
    public async Task<IActionResult> ReanalyzeFile(int fileId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _fileService.ReanalyzeFileAsync(fileId, userId);
            if (result)
            {
                return Ok(new { success = true, message = "文件重新分析成功" });
            }
            else
            {
                return BadRequest(new { success = false, message = "文件重新分析失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新分析文件失败: {FileId}", fileId);
            return StatusCode(500, new { success = false, message = "服务器错误" });
        }
    }

    /// <summary>
    /// 获取文件的AI分析结果
    /// </summary>
    [HttpGet("analysis/{fileId}")]
    public async Task<IActionResult> GetFileAnalysis(int fileId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var analysis = await _fileService.GetFileAnalysisAsync(fileId, userId);
            if (analysis != null)
            {
                return Ok(analysis);
            }
            else
            {
                return NotFound(new { message = "未找到AI分析结果" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取AI分析结果失败: {FileId}", fileId);
            return StatusCode(500, new { message = "服务器错误" });
        }
    }

    /// <summary>
    /// 根据类别搜索文件
    /// </summary>
    [HttpGet("search/category/{category}")]
    public async Task<IActionResult> SearchByCategory(string category)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var files = await _fileService.SearchFilesByCategoryAsync(category, userId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类别搜索文件失败: {Category}", category);
            return StatusCode(500, new { message = "服务器错误" });
        }
    }

    /// <summary>
    /// 根据标签搜索文件
    /// </summary>
    [HttpGet("search/tag/{tag}")]
    public async Task<IActionResult> SearchByTag(string tag)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var files = await _fileService.SearchFilesByTagAsync(tag, userId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按标签搜索文件失败: {Tag}", tag);
            return StatusCode(500, new { message = "服务器错误" });
        }
    }

    /// <summary>
    /// 获取所有AI分析的文档类别统计
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var files = await _fileService.GetUserFilesAsync(userId);
            var categories = files
                .Where(f => !string.IsNullOrEmpty(f.AiCategory))
                .GroupBy(f => f.AiCategory)
                .Select(g => new { category = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文档类别统计失败");
            return StatusCode(500, new { message = "服务器错误" });
        }
    }

    /// <summary>
    /// 获取所有AI分析的标签统计
    /// </summary>
    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var files = await _fileService.GetUserFilesAsync(userId);
            var allTags = new List<string>();
            
            foreach (var file in files.Where(f => !string.IsNullOrEmpty(f.AiTags)))
            {
                var tags = file.AiTags!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t));
                allTags.AddRange(tags);
            }

            var tagStats = allTags
                .GroupBy(t => t)
                .Select(g => new { tag = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(20) // 取前20个最常用的标签
                .ToList();

            return Ok(tagStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取标签统计失败");
            return StatusCode(500, new { message = "服务器错误" });
        }
    }

    /// <summary>
    /// 获取AI分析统计数据
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var files = await _fileService.GetUserFilesAsync(userId);
            
            var totalFiles = files.Count;
            var analyzedFiles = files.Count(f => !string.IsNullOrEmpty(f.AiSummary) || !string.IsNullOrEmpty(f.AiCategory));
            var pendingFiles = totalFiles - analyzedFiles;
            
            var allTags = new List<string>();
            foreach (var file in files.Where(f => !string.IsNullOrEmpty(f.AiTags)))
            {
                var tags = file.AiTags!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t));
                allTags.AddRange(tags);
            }
            var totalTags = allTags.Distinct().Count();

            return Ok(new
            {
                totalFiles = totalFiles,
                analyzedFiles = analyzedFiles,
                pendingFiles = pendingFiles,
                totalTags = totalTags
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取AI统计数据失败");
            return StatusCode(500, new { message = "服务器错误" });
        }
    }

    /// <summary>
    /// 批量分析文件
    /// </summary>
    [HttpPost("batch-analyze")]
    public async Task<IActionResult> BatchAnalyzeFiles([FromBody] List<int> fileIds)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (fileIds == null || !fileIds.Any())
        {
            return BadRequest(new { message = "请选择要分析的文件" });
        }

        try
        {
            // 验证文件所有权
            var userFiles = await _fileService.GetUserFilesAsync(userId);
            var validFileIds = fileIds.Where(id => userFiles.Any(f => f.Id == id)).ToList();

            if (!validFileIds.Any())
            {
                return BadRequest(new { message = "未找到有效的文件" });
            }

            // 异步批量分析
            var tasks = validFileIds.Select(async fileId =>
            {
                try
                {
                    await _fileService.AnalyzeFileWithAiAsync(fileId);
                    return new { fileId, success = true };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量分析文件失败: {FileId}", fileId);
                    return new { fileId, success = false };
                }
            });

            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r.success);
            var failCount = results.Count(r => !r.success);

            return Ok(new
            {
                success = true,
                message = $"批量分析完成，成功: {successCount}，失败: {failCount}",
                successCount,
                failCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量分析文件时出错");
            return StatusCode(500, new { message = "服务器错误" });
        }
    }

    /// <summary>
    /// 获取文件AI分析状态
    /// </summary>
    [HttpGet("status/{fileId}")]
    public async Task<IActionResult> GetAnalysisStatus(int fileId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            if (file == null)
            {
                return NotFound(new { message = "文件不存在" });
            }

            var hasAnalysis = !string.IsNullOrEmpty(file.AiSummary) || !string.IsNullOrEmpty(file.AiCategory);
            
            return Ok(new
            {
                fileId = file.Id,
                fileName = file.FileName,
                hasAnalysis,
                aiSummary = file.AiSummary,
                aiCategory = file.AiCategory,
                aiTags = file.AiTags,
                lastAnalysis = file.LastAiAnalysis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件AI分析状态失败: {FileId}", fileId);
            return StatusCode(500, new { message = "服务器错误" });
        }
    }
} 