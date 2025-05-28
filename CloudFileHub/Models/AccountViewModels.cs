using System.ComponentModel.DataAnnotations;

namespace CloudFileHub.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "请输入电子邮箱")]
    [EmailAddress(ErrorMessage = "请输入有效的电子邮箱地址")]
    [Display(Name = "电子邮箱")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "请输入密码")]
    [StringLength(100, ErrorMessage = "{0}必须至少{2}个字符，最多{1}个字符。", MinimumLength = 4)]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;
    
    [DataType(DataType.Password)]
    [Display(Name = "确认密码")]
    [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Display(Name = "名字")]
    public string? FirstName { get; set; }
    
    [Display(Name = "姓氏")]
    public string? LastName { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "请输入电子邮箱")]
    [EmailAddress(ErrorMessage = "请输入有效的电子邮箱地址")]
    [Display(Name = "电子邮箱")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;
    
    [Display(Name = "记住我?")]
    public bool RememberMe { get; set; }
}

public class ProfileViewModel
{
    [EmailAddress(ErrorMessage = "请输入有效的电子邮箱地址")]
    [Display(Name = "电子邮箱")]
    public string? Email { get; set; }
    
    [Display(Name = "名字")]
    public string? FirstName { get; set; }
    
    [Display(Name = "姓氏")]
    public string? LastName { get; set; }
    
    [Display(Name = "已使用存储空间")]
    public long StorageUsed { get; set; }
    
    [Display(Name = "存储空间限制")]
    public long StorageLimit { get; set; }
    
    [Display(Name = "已使用存储空间 (MB)")]
    public double StorageUsedMB => Math.Round((double)StorageUsed / (1024 * 1024), 2);
    
    [Display(Name = "存储空间限制 (MB)")]
    public double StorageLimitMB => Math.Round((double)StorageLimit / (1024 * 1024), 2);
    
    [Display(Name = "存储空间使用率 (%)")]
    public double StorageUsagePercentage => StorageLimit > 0 ? Math.Round((double)StorageUsed / StorageLimit * 100, 2) : 0;
}
