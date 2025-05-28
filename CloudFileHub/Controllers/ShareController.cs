using CloudFileHub.Models;
using CloudFileHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CloudFileHub.Controllers;

public class ShareController : Controller
{
    private readonly ShareService _shareService;
    private readonly FileService _fileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AliyunOSSService _ossService;
    
    public ShareController(
        ShareService shareService,
        FileService fileService,
        UserManager<ApplicationUser> userManager,
        AliyunOSSService ossService)
    {
        _shareService = shareService;
        _fileService = fileService;
        _userManager = userManager;
        _ossService = ossService;
    }
    
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }
        
        var shares = await _shareService.GetUserSharesAsync(userId);
        return View(shares);
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(int fileId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }
        
        var file = await _fileService.GetFileByIdAsync(fileId, userId);
        if (file == null)
        {
            return NotFound();
        }
        
        ViewBag.File = file;
        return View();
    }
    
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int fileId, DateTime? expiryDate, bool isPasswordProtected, 
        string? password, int? maxDownloads)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }
        
        if (isPasswordProtected && string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Password is required when password protection is enabled.");
            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            ViewBag.File = file;
            return View();
        }
        
        try
        {
            var share = await _shareService.CreateShareLinkAsync(fileId, userId, expiryDate, 
                isPasswordProtected, password, maxDownloads);
                
            return RedirectToAction(nameof(Details), new { id = share.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            ViewBag.File = file;
            return View();
        }
    }
    
    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }
        
        var shares = await _shareService.GetUserSharesAsync(userId);
        var share = shares.FirstOrDefault(s => s.Id == id);
        
        if (share == null)
        {
            return NotFound();
        }
        
        // Generate share URL
        var shareUrl = Url.Action("Download", "Share", new { code = share.ShareCode }, Request.Scheme);
        ViewBag.ShareUrl = shareUrl;
        
        return View(share);
    }
    
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }
        
        var result = await _shareService.DeleteShareLinkAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet]
    public IActionResult Download(string code)
    {
        ViewBag.ShareCode = code;
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Download(string code, string? password)
    {
        var file = await _shareService.GetSharedFileAsync(code, password);
        if (file == null)
        {
            ModelState.AddModelError("", "Invalid share link or password.");
            ViewBag.ShareCode = code;
            return View();
        }
        
        // Download file from OSS using the file path (which is the OSS key)
        var stream = await _ossService.DownloadFileAsync(file.FilePath);
        if (stream == null)
        {
            ModelState.AddModelError("", "File not found on storage.");
            ViewBag.ShareCode = code;
            return View();
        }
        
        // Increment download count
        await _shareService.IncrementDownloadCountAsync(code);
        
        return File(stream, file.ContentType, file.FileName);
    }
}
