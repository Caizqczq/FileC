using FileC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FileC.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    
    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }
    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };
            
            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "File");
            }
            
            foreach (var error in result.Errors)
            {
                string errorMessage = error.Description;
                // 将英文错误消息转换为中文
                if (error.Code == "DuplicateUserName")
                {
                    errorMessage = "该电子邮箱已被注册。";
                }
                else if (error.Code == "PasswordRequiresNonAlphanumeric")
                {
                    errorMessage = "密码必须包含至少一个特殊字符。";
                }
                else if (error.Code == "PasswordRequiresDigit")
                {
                    errorMessage = "密码必须包含至少一个数字。";
                }
                else if (error.Code == "PasswordRequiresLower")
                {
                    errorMessage = "密码必须包含至少一个小写字母。";
                }
                else if (error.Code == "PasswordRequiresUpper")
                {
                    errorMessage = "密码必须包含至少一个大写字母。";
                }
                else if (error.Code.StartsWith("Password"))
                {
                    errorMessage = "密码不符合要求。";
                }
                
                ModelState.AddModelError("", errorMessage);
            }
        }
        
        return View(model);
    }
    
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            
            ModelState.AddModelError(string.Empty, "登录尝试无效。请检查您的用户名和密码。");
        }
        
        return View(model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("无法找到用户。");
        }
        
        var model = new ProfileViewModel
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            StorageUsed = user.StorageUsed,
            StorageLimit = user.StorageLimit
        };
        
        return View(model);
    }
    
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "File");
        }
    }
}
