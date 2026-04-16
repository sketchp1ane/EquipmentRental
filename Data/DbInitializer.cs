using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db          = services.GetRequiredService<AppDbContext>();

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

        await SeedCategoriesAsync(db);
    }

    // ── 预置设备分类 ──────────────────────────────────────────────────────────

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        if (await db.EquipmentCategories.AnyAsync()) return;

        // 一级分类
        var roots = new[]
        {
            new EquipmentCategory { Name = "起重机械",   Level = 1, SortOrder = 1 },
            new EquipmentCategory { Name = "土石方机械", Level = 1, SortOrder = 2 },
            new EquipmentCategory { Name = "混凝土机械", Level = 1, SortOrder = 3 },
            new EquipmentCategory { Name = "桩工机械",   Level = 1, SortOrder = 4 },
            new EquipmentCategory { Name = "高空作业机械",Level = 1, SortOrder = 5 },
            new EquipmentCategory { Name = "钢筋加工机械",Level = 1, SortOrder = 6 },
            new EquipmentCategory { Name = "焊接设备",   Level = 1, SortOrder = 7 },
            new EquipmentCategory { Name = "动力与电气",  Level = 1, SortOrder = 8 },
        };
        db.EquipmentCategories.AddRange(roots);
        await db.SaveChangesAsync();

        // 二级分类（按父级名称关联，避免硬编码 ID）
        var sub = new[]
        {
            // 起重机械
            ("起重机械", "塔式起重机",     1),
            ("起重机械", "汽车起重机",     2),
            ("起重机械", "履带起重机",     3),
            ("起重机械", "施工升降机",     4),
            ("起重机械", "物料提升机",     5),

            // 土石方机械
            ("土石方机械", "挖掘机",       1),
            ("土石方机械", "推土机",       2),
            ("土石方机械", "装载机",       3),
            ("土石方机械", "压路机",       4),
            ("土石方机械", "平地机",       5),

            // 混凝土机械
            ("混凝土机械", "混凝土搅拌机", 1),
            ("混凝土机械", "混凝土泵车",   2),
            ("混凝土机械", "混凝土振捣棒", 3),

            // 桩工机械
            ("桩工机械", "旋挖钻机",       1),
            ("桩工机械", "振动打桩机",     2),
            ("桩工机械", "静压桩机",       3),

            // 高空作业机械
            ("高空作业机械", "剪叉式升降台",  1),
            ("高空作业机械", "直臂式高空车",  2),
            ("高空作业机械", "曲臂式高空车",  3),

            // 钢筋加工机械
            ("钢筋加工机械", "钢筋弯曲机",   1),
            ("钢筋加工机械", "钢筋切断机",   2),
            ("钢筋加工机械", "钢筋调直机",   3),

            // 焊接设备
            ("焊接设备", "电弧焊机",        1),
            ("焊接设备", "氩弧焊机",        2),
            ("焊接设备", "气体保护焊机",    3),

            // 动力与电气
            ("动力与电气", "发电机组",      1),
            ("动力与电气", "配电箱",        2),
            ("动力与电气", "电缆盘",        3),
        };

        var rootMap = roots.ToDictionary(r => r.Name, r => r.Id);

        foreach (var (parentName, childName, sort) in sub)
        {
            db.EquipmentCategories.Add(new EquipmentCategory
            {
                Name      = childName,
                Level     = 2,
                SortOrder = sort,
                ParentId  = rootMap[parentName]
            });
        }

        await db.SaveChangesAsync();
    }
}
