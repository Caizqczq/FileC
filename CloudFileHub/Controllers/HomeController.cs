using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CloudFileHub.Models;
using CloudFileHub.Services;
using Microsoft.AspNetCore.Identity;

namespace CloudFileHub.Controllers;

public class HomeController : Controller
{
    private readonly FileService _fileService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        FileService fileService,
        UserManager<ApplicationUser> userManager)
    {
        _fileService = fileService;
        _userManager = userManager;
    }

    public IActionResult Index(int? directoryId = null)
    {
        // 如果用户已登录，重定向到仪表盘；否则重定向到文件页面
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return RedirectToAction("Index", "File", new { directoryId });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
