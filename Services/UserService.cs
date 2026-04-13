using EquipmentRental.Data;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor)
{
    // ── Audit Logging ─────────────────────────────────────────────────────────

    private string? ClientIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private async Task WriteOperationLogAsync(
        string actorUserId, string action, string entityId, string? detail = null)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId     = actorUserId,
            Action     = action,
            EntityType = "User",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    public async Task LogLockoutEventAsync(string targetUserId, string? clientIp)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId     = targetUserId,
            Action     = "LockoutTriggered",
            EntityType = "User",
            EntityId   = targetUserId,
            Detail     = "账号因多次密码错误被锁定30分钟",
            OccurredAt = DateTime.UtcNow,
            ClientIp   = clientIp
        });
        await db.SaveChangesAsync();
    }

    // ── User List (Paged) ─────────────────────────────────────────────────────

    public async Task<UserListViewModel> GetPagedUsersAsync(
        string? keyword, string? role, bool? isActive, int page, int pageSize = 15)
    {
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u =>
                u.RealName.Contains(keyword) ||
                (u.UserName != null && u.UserName.Contains(keyword)));

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        // Role filter — join via UserRoles
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleEntity = await roleManager.FindByNameAsync(role);
            if (roleEntity != null)
            {
                var userIdsInRole = db.UserRoles
                    .Where(ur => ur.RoleId == roleEntity.Id)
                    .Select(ur => ur.UserId);
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
            else
            {
                // Role not found — return empty
                return new UserListViewModel
                {
                    Keyword = keyword, Role = role, IsActive = isActive,
                    Page = page, TotalPages = 0,
                    AllRoles = Constants.Roles.All
                };
            }
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Bulk-load roles for this page (avoid N+1)
        var userIds = users.Select(u => u.Id).ToHashSet();
        var roleDict = await (
            from ur in db.UserRoles
            join r  in db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, r.Name }
        ).ToListAsync();

        var roleLookup = roleDict
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IList<string>)g.Select(x => x.Name!).ToList());

        var items = users.Select(u => new UserListItemViewModel
        {
            Id          = u.Id,
            UserName    = u.UserName ?? string.Empty,
            RealName    = u.RealName,
            Email       = u.Email,
            PhoneNumber = u.PhoneNumber,
            IsActive    = u.IsActive,
            CreatedAt   = u.CreatedAt,
            Roles       = roleLookup.TryGetValue(u.Id, out var roles) ? roles : []
        }).ToList();

        return new UserListViewModel
        {
            Items      = items,
            Keyword    = keyword,
            Role       = role,
            IsActive   = isActive,
            Page       = page,
            TotalPages = totalPages,
            AllRoles   = Constants.Roles.All
        };
    }

    // ── Get User for Edit ────────────────────────────────────────────────────

    public async Task<EditUserViewModel?> GetUserForEditAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return null;

        var currentRoles = await userManager.GetRolesAsync(user);
        return new EditUserViewModel
        {
            Id           = user.Id,
            UserName     = user.UserName ?? string.Empty,
            RealName     = user.RealName,
            PhoneNumber  = user.PhoneNumber,
            Email        = user.Email,
            IsActive     = user.IsActive,
            SelectedRoles = currentRoles.ToList(),
            AllRoles     = Constants.Roles.All
        };
    }

    // ── Create User ───────────────────────────────────────────────────────────

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateUserAsync(
        CreateUserViewModel vm, string actorUserId)
    {
        var user = new ApplicationUser
        {
            UserName     = vm.UserName,
            NormalizedUserName = vm.UserName.ToUpperInvariant(),
            Email        = vm.Email,
            NormalizedEmail = vm.Email != null ? userManager.NormalizeEmail(vm.Email) : null,
            RealName     = vm.RealName,
            PhoneNumber  = vm.PhoneNumber,
            IsActive     = vm.IsActive,
            CreatedAt    = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        if (vm.SelectedRoles.Count > 0)
            await userManager.AddToRolesAsync(user, vm.SelectedRoles);

        await WriteOperationLogAsync(actorUserId, "CreateUser", user.Id,
            $"UserName={vm.UserName}, Roles={string.Join(",", vm.SelectedRoles)}");

        return (true, []);
    }

    // ── Update User ───────────────────────────────────────────────────────────

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(
        EditUserViewModel vm, string actorUserId)
    {
        var user = await userManager.FindByIdAsync(vm.Id);
        if (user == null)
            return (false, ["用户不存在"]);

        user.RealName    = vm.RealName;
        user.PhoneNumber = vm.PhoneNumber;
        user.Email       = vm.Email;
        user.NormalizedEmail = vm.Email != null ? userManager.NormalizeEmail(vm.Email) : null;
        user.IsActive    = vm.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, updateResult.Errors.Select(e => e.Description));

        // Sync roles
        var currentRoles = await userManager.GetRolesAsync(user);
        var toAdd    = vm.SelectedRoles.Except(currentRoles).ToList();
        var toRemove = currentRoles.Except(vm.SelectedRoles).ToList();

        if (toAdd.Count > 0)
            await userManager.AddToRolesAsync(user, toAdd);
        if (toRemove.Count > 0)
            await userManager.RemoveFromRolesAsync(user, toRemove);

        await WriteOperationLogAsync(actorUserId, "UpdateUser", vm.Id,
            $"RealName={vm.RealName}, IsActive={vm.IsActive}, Roles={string.Join(",", vm.SelectedRoles)}");

        return (true, []);
    }

    // ── Toggle Active ─────────────────────────────────────────────────────────

    public async Task<(bool Success, bool NewIsActive, string? Error)> ToggleActiveAsync(
        string targetUserId, string actorUserId)
    {
        if (targetUserId == actorUserId)
            return (false, false, "不能停用自己的账号");

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return (false, false, "用户不存在");

        user.IsActive = !user.IsActive;
        await userManager.UpdateAsync(user);

        await WriteOperationLogAsync(actorUserId, "ToggleActive", targetUserId,
            $"IsActive={user.IsActive}");

        return (true, user.IsActive, null);
    }

    // ── Reset Password (Admin) ────────────────────────────────────────────────

    public async Task<(bool Success, IEnumerable<string> Errors)> ResetPasswordAsync(
        string targetUserId, string newPassword, string actorUserId)
    {
        var user = await userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return (false, ["用户不存在"]);

        var token  = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        await WriteOperationLogAsync(actorUserId, "AdminResetPassword", targetUserId,
            $"Target={user.UserName}");

        return (true, []);
    }

    // ── Profile (Self-Service) ────────────────────────────────────────────────

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateProfileAsync(
        string userId, ProfileViewModel vm)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, ["用户不存在"]);

        user.RealName    = vm.RealName;
        user.PhoneNumber = vm.PhoneNumber;
        user.Email       = vm.Email;
        user.NormalizedEmail = vm.Email != null ? userManager.NormalizeEmail(vm.Email) : null;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        await WriteOperationLogAsync(userId, "UpdateProfile", userId,
            $"RealName={vm.RealName}");

        return (true, []);
    }

    // ── Change Password (Self-Service) ────────────────────────────────────────

    public async Task<(bool Success, IEnumerable<string> Errors)> ChangePasswordAsync(
        string userId, ChangePasswordViewModel vm)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, ["用户不存在"]);

        var result = await userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        await WriteOperationLogAsync(userId, "ChangePassword", userId, null);

        return (true, []);
    }

    // ── Profile ViewModel Helper ──────────────────────────────────────────────

    public async Task<ProfileViewModel?> GetProfileAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return null;

        return new ProfileViewModel
        {
            RealName    = user.RealName,
            PhoneNumber = user.PhoneNumber,
            Email       = user.Email
        };
    }

    // ── ResetPassword ViewModel Helper ────────────────────────────────────────

    public async Task<ResetPasswordViewModel?> GetResetPasswordViewModelAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return null;

        return new ResetPasswordViewModel
        {
            UserId   = user.Id,
            UserName = user.UserName ?? string.Empty
        };
    }
}
