using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudFileHub.Models;
using CloudFileHub.Services;

namespace CloudFileHub.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        DashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _userManager = userManager;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// 仪表盘首页
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var dashboardData = await _dashboardService.GetDashboardDataAsync(userId);
            return View(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "仪表盘页面加载失败");
            TempData["ErrorMessage"] = "仪表盘数据加载失败，请刷新页面重试。";
            return View();
        }
    }

    /// <summary>
    /// 获取存储统计API
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStorageStats()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var dashboardData = await _dashboardService.GetDashboardDataAsync(userId);
            return Json(new
            {
                labels = dashboardData.StorageStats.FileTypeStorage.Keys.ToArray(),
                data = dashboardData.StorageStats.FileTypeStorage.Values.ToArray(),
                counts = dashboardData.StorageStats.FileTypeCount.Values.ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取存储统计失败");
            return Json(new { error = "获取数据失败" });
        }
    }
} 