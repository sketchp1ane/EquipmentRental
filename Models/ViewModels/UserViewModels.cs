using System.ComponentModel.DataAnnotations;

namespace EquipmentRental.Models.ViewModels;

public class UserListViewModel
{
    public IList<UserListItemViewModel> Items { get; set; } = [];
    public string? Keyword { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public IList<string> AllRoles { get; set; } = [];
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public IList<string> Roles { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "请输入用户名")]
    [StringLength(256, ErrorMessage = "用户名不能超过256个字符")]
    [Display(Name = "用户名")]
    public string UserName { get; set; } = string.Empty;

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

    [Required(ErrorMessage = "请输入初始密码")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度至少8位")]
    [DataType(DataType.Password)]
    [Display(Name = "初始密码")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "启用账号")]
    public bool IsActive { get; set; } = true;

    public List<string> SelectedRoles { get; set; } = [];

    // Populated by controller, not bound from form
    public IList<string> AllRoles { get; set; } = [];
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "用户名")]
    public string UserName { get; set; } = string.Empty;

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

    [Display(Name = "启用账号")]
    public bool IsActive { get; set; }

    public List<string> SelectedRoles { get; set; } = [];

    // Populated by controller, not bound from form
    public IList<string> AllRoles { get; set; } = [];
}

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Display(Name = "用户名")]
    public string UserName { get; set; } = string.Empty;

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
