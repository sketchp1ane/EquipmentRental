using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace EquipmentRental.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        const string adminEmail = "admin@equiprental.com";
        const string adminPassword = "Admin@123456";

        var admin = await userManager.FindByNameAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                RealName = "系统管理员",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new Exception(
                    $"创建管理员账号失败：{string.Join(", ", result.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
        else if (admin.PasswordHash != null && !admin.PasswordHash.StartsWith("$2"))
        {
            // Migrate from PBKDF2 to BCrypt on first startup after hasher swap
            await userManager.RemovePasswordAsync(admin);
            await userManager.AddPasswordAsync(admin, adminPassword);
        }
    }
}
