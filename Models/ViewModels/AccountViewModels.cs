using System.ComponentModel.DataAnnotations;

namespace EquipmentRental.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "请输入用户名")]
    [Display(Name = "用户名")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "记住我（7天）")]
    public bool RememberMe { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "请输入真实姓名")]
    [StringLength(50, ErrorMessage = "姓名不能超过50个字符")]
    [Display(Name = "真实姓名")]
    public string RealName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "手机号格式不正确")]
    [Display(Name = "手机号")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [Display(Name = "邮箱")]
    public string? Email { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "请输入当前密码")]
    [DataType(DataType.Password)]
    [Display(Name = "当前密码")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入新密码")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度至少8位")]
    [DataType(DataType.Password)]
    [Display(Name = "新密码")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "请确认新密码")]
    [Compare(nameof(NewPassword), ErrorMessage = "两次输入的密码不一致")]
    [DataType(DataType.Password)]
    [Display(Name = "确认新密码")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
