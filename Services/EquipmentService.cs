using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace EquipmentRental.Services;

public class EquipmentService(
    AppDbContext db,
    FileService fileService,
    IHttpContextAccessor httpContextAccessor)
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? ClientIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private async Task WriteOperationLogAsync(
        string actorUserId, string action, string entityId, string? detail = null)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId     = actorUserId,
            Action     = action,
            EntityType = "Equipment",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    private IQueryable<Equipment> BuildFilterQuery(
        int? categoryId, EquipmentStatus? status, string? keyword)
    {
        var query = db.Equipments.AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(e =>
                e.EquipmentNo.Contains(keyword) ||
                e.Name.Contains(keyword) ||
                e.BrandModel.Contains(keyword));

        return query;
    }

    private static string StatusToString(EquipmentStatus s) => s switch
    {
        EquipmentStatus.PendingReview => "待审核",
        EquipmentStatus.Idle          => "闲置",
        EquipmentStatus.InUse         => "使用中",
        EquipmentStatus.Maintenance   => "维修中",
        EquipmentStatus.Scrapped      => "已报废",
        _                             => s.ToString()
    };

    // ── Category Methods ──────────────────────────────────────────────────────

    public async Task<IList<CategoryListItemViewModel>> GetCategoryListAsync()
    {
        var all = await db.EquipmentCategories
            .OrderBy(c => c.Level)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var nameMap = all.ToDictionary(c => c.Id, c => c.Name);

        // Batch check which categories have equipment (avoid N+1)
        var categoriesWithEquipment = await db.Equipments
            .GroupBy(e => e.CategoryId)
            .Select(g => g.Key)
            .ToHashSetAsync();

        return all.Select(c => new CategoryListItemViewModel
        {
            Id           = c.Id,
            Name         = c.Name,
            ParentName   = c.ParentId.HasValue
                               ? nameMap.GetValueOrDefault(c.ParentId.Value)
                               : null,
            Level        = c.Level,
            SortOrder    = c.SortOrder,
            HasChildren  = all.Any(x => x.ParentId == c.Id),
            HasEquipments = categoriesWithEquipment.Contains(c.Id)
        }).ToList();
    }

    public async Task<IList<SelectListItem>> GetCategorySelectListAsync()
    {
        var all = await db.EquipmentCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var roots    = all.Where(c => c.ParentId == null).ToList();
        var children = all.Where(c => c.ParentId != null).ToList();

        var groups            = roots.ToDictionary(r => r.Id, r => new SelectListGroup { Name = r.Name });
        var rootsWithChildren = children.Select(c => c.ParentId!.Value).ToHashSet();

        var items = new List<SelectListItem>();
        foreach (var root in roots)
        {
            if (!rootsWithChildren.Contains(root.Id))
            {
                // 无子分类的一级分类直接可选
                items.Add(new SelectListItem { Value = root.Id.ToString(), Text = root.Name });
                continue;
            }

            foreach (var child in children.Where(c => c.ParentId == root.Id)
                                          .OrderBy(c => c.SortOrder).ThenBy(c => c.Name))
            {
                items.Add(new SelectListItem
                {
                    Value = child.Id.ToString(),
                    Text  = child.Name,
                    Group = groups[root.Id]
                });
            }
        }
        return items;
    }

    public async Task<CategoryFormViewModel?> GetCategoryForEditAsync(int id)
    {
        var category = await db.EquipmentCategories.FindAsync(id);
        if (category == null) return null;

        return new CategoryFormViewModel
        {
            Id            = category.Id,
            Name          = category.Name,
            ParentId      = category.ParentId,
            Level         = category.Level,
            SortOrder     = category.SortOrder,
            ParentOptions = await GetCategorySelectListAsync()
        };
    }

    public async Task<(bool Success, string? Error)> CreateCategoryAsync(
        CategoryFormViewModel vm, string actorUserId)
    {
        var category = new EquipmentCategory
        {
            Name      = vm.Name,
            ParentId  = vm.ParentId,
            Level     = vm.Level,
            SortOrder = vm.SortOrder
        };

        db.EquipmentCategories.Add(category);
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(actorUserId, "CreateCategory",
            category.Id.ToString(), $"Name={vm.Name}");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateCategoryAsync(
        CategoryFormViewModel vm, string actorUserId)
    {
        var category = await db.EquipmentCategories.FindAsync(vm.Id);
        if (category == null) return (false, "分类不存在");

        // Prevent setting parent to self
        if (vm.ParentId == vm.Id)
            return (false, "不能将自身设为上级分类");

        category.Name      = vm.Name;
        category.ParentId  = vm.ParentId;
        category.Level     = vm.Level;
        category.SortOrder = vm.SortOrder;

        await db.SaveChangesAsync();
        await WriteOperationLogAsync(actorUserId, "UpdateCategory",
            category.Id.ToString(), $"Name={vm.Name}");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAsync(
        int id, string actorUserId)
    {
        var hasChildren = await db.EquipmentCategories.AnyAsync(c => c.ParentId == id);
        if (hasChildren) return (false, "该分类下存在子分类，无法删除");

        var hasEquipments = await db.Equipments.AnyAsync(e => e.CategoryId == id);
        if (hasEquipments) return (false, "该分类下存在设备，无法删除");

        var category = await db.EquipmentCategories.FindAsync(id);
        if (category == null) return (false, "分类不存在");

        db.EquipmentCategories.Remove(category);
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(actorUserId, "DeleteCategory",
            id.ToString(), $"Name={category.Name}");

        return (true, null);
    }

    // ── Equipment List ────────────────────────────────────────────────────────

    public async Task<EquipmentListViewModel> GetPagedEquipmentsAsync(
        int? categoryId, EquipmentStatus? status, string? keyword,
        int page, int pageSize = 15)
    {
        var query = BuildFilterQuery(categoryId, status, keyword);

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EquipmentListItemViewModel
            {
                Id           = e.Id,
                EquipmentNo  = e.EquipmentNo,
                Name         = e.Name,
                CategoryName = e.Category.Name,
                BrandModel   = e.BrandModel,
                Status       = e.Status,
                CreatedAt    = e.CreatedAt
            })
            .ToListAsync();

        return new EquipmentListViewModel
        {
            Items           = items,
            CategoryId      = categoryId,
            Status          = status,
            Keyword         = keyword,
            Page            = page,
            TotalPages      = totalPages,
            TotalCount      = total,
            CategoryOptions = await GetCategorySelectListAsync()
        };
    }

    // ── Equipment Create ──────────────────────────────────────────────────────

    public async Task<CreateEquipmentViewModel> GetCreateViewModelAsync()
    {
        return new CreateEquipmentViewModel
        {
            CategoryOptions = await GetCategorySelectListAsync()
        };
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateEquipmentAsync(
        CreateEquipmentViewModel vm, string actorUserId)
    {
        // Check EquipmentNo uniqueness
        bool exists = await db.Equipments.AnyAsync(e => e.EquipmentNo == vm.EquipmentNo);
        if (exists) return (false, ["设备编号已存在"]);

        var equipment = new Equipment
        {
            EquipmentNo     = vm.EquipmentNo,
            Name            = vm.Name,
            CategoryId      = vm.CategoryId,
            BrandModel      = vm.BrandModel,
            ManufactureDate = vm.ManufactureDate,
            FactoryNo       = vm.FactoryNo,
            TechSpecs       = vm.TechSpecs,
            PurchaseDate    = vm.PurchaseDate,
            OriginalValue   = vm.OriginalValue,
            OwnedBy         = vm.OwnedBy,
            Status          = vm.Status,
            Remark          = vm.Remark,
            CreatedById     = actorUserId,
            CreatedAt       = DateTime.UtcNow
        };

        db.Equipments.Add(equipment);
        await db.SaveChangesAsync();

        // Upload images atomically
        try
        {
            if (vm.Images != null && vm.Images.Count > 0)
            {
                var imagePaths = new List<string>();
                foreach (var file in vm.Images)
                    imagePaths.Add(await fileService.SaveFileAsync(file, "Equipment"));

                foreach (var path in imagePaths)
                    db.EquipmentImages.Add(new EquipmentImage
                    {
                        EquipmentId = equipment.Id,
                        FilePath    = path,
                        UploadedAt  = DateTime.UtcNow
                    });

                await db.SaveChangesAsync();
            }
        }
        catch (InvalidOperationException ex)
        {
            return (false, [ex.Message]);
        }

        await WriteOperationLogAsync(actorUserId, "CreateEquipment",
            equipment.Id.ToString(), $"EquipmentNo={vm.EquipmentNo}");

        return (true, []);
    }

    // ── Equipment Edit ────────────────────────────────────────────────────────

    public async Task<EditEquipmentViewModel?> GetEquipmentForEditAsync(int id)
    {
        var e = await db.Equipments
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e == null) return null;

        return new EditEquipmentViewModel
        {
            Id              = e.Id,
            EquipmentNo     = e.EquipmentNo,
            Name            = e.Name,
            CategoryId      = e.CategoryId,
            BrandModel      = e.BrandModel,
            ManufactureDate = e.ManufactureDate,
            FactoryNo       = e.FactoryNo,
            TechSpecs       = e.TechSpecs,
            PurchaseDate    = e.PurchaseDate,
            OriginalValue   = e.OriginalValue,
            OwnedBy         = e.OwnedBy,
            Status          = e.Status,
            Remark          = e.Remark,
            CategoryOptions = await GetCategorySelectListAsync(),
            ExistingImages  = e.Images.Select(i => new EquipmentImageViewModel
            {
                Id         = i.Id,
                FilePath   = i.FilePath,
                UploadedAt = i.UploadedAt
            }).ToList()
        };
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateEquipmentAsync(
        EditEquipmentViewModel vm, string actorUserId)
    {
        var equipment = await db.Equipments.FindAsync(vm.Id);
        if (equipment == null) return (false, ["设备不存在"]);

        // Check EquipmentNo uniqueness only if changed
        if (equipment.EquipmentNo != vm.EquipmentNo)
        {
            bool dup = await db.Equipments
                .AnyAsync(e => e.EquipmentNo == vm.EquipmentNo && e.Id != vm.Id);
            if (dup) return (false, ["设备编号已被其他设备使用"]);
        }

        equipment.EquipmentNo     = vm.EquipmentNo;
        equipment.Name            = vm.Name;
        equipment.CategoryId      = vm.CategoryId;
        equipment.BrandModel      = vm.BrandModel;
        equipment.ManufactureDate = vm.ManufactureDate;
        equipment.FactoryNo       = vm.FactoryNo;
        equipment.TechSpecs       = vm.TechSpecs;
        equipment.PurchaseDate    = vm.PurchaseDate;
        equipment.OriginalValue   = vm.OriginalValue;
        equipment.OwnedBy         = vm.OwnedBy;
        equipment.Status          = vm.Status;
        equipment.Remark          = vm.Remark;

        // Delete selected images (security: filter by EquipmentId)
        if (vm.DeleteImageIds.Count > 0)
        {
            var toDelete = await db.EquipmentImages
                .Where(i => vm.DeleteImageIds.Contains(i.Id) && i.EquipmentId == vm.Id)
                .ToListAsync();
            db.EquipmentImages.RemoveRange(toDelete);
        }

        await db.SaveChangesAsync();

        // Upload new images
        try
        {
            if (vm.Images != null && vm.Images.Count > 0)
            {
                var imagePaths = new List<string>();
                foreach (var file in vm.Images)
                    imagePaths.Add(await fileService.SaveFileAsync(file, "Equipment"));

                foreach (var path in imagePaths)
                    db.EquipmentImages.Add(new EquipmentImage
                    {
                        EquipmentId = equipment.Id,
                        FilePath    = path,
                        UploadedAt  = DateTime.UtcNow
                    });

                await db.SaveChangesAsync();
            }
        }
        catch (InvalidOperationException ex)
        {
            return (false, [ex.Message]);
        }

        await WriteOperationLogAsync(actorUserId, "UpdateEquipment",
            equipment.Id.ToString(), $"EquipmentNo={vm.EquipmentNo}");

        return (true, []);
    }

    // ── Equipment Details ─────────────────────────────────────────────────────

    public async Task<EquipmentDetailsViewModel?> GetEquipmentDetailsAsync(int id)
    {
        var e = await db.Equipments
            .Include(x => x.Category)
            .Include(x => x.CreatedBy)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e == null) return null;

        return new EquipmentDetailsViewModel
        {
            Id            = e.Id,
            EquipmentNo   = e.EquipmentNo,
            Name          = e.Name,
            CategoryName  = e.Category.Name,
            BrandModel    = e.BrandModel,
            ManufactureDate = e.ManufactureDate,
            FactoryNo     = e.FactoryNo,
            TechSpecs     = e.TechSpecs,
            PurchaseDate  = e.PurchaseDate,
            OriginalValue = e.OriginalValue,
            OwnedBy       = e.OwnedBy,
            Status        = e.Status,
            Remark        = e.Remark,
            CreatedByName = e.CreatedBy.RealName,
            CreatedAt     = e.CreatedAt,
            Images        = e.Images.Select(i => new EquipmentImageViewModel
            {
                Id         = i.Id,
                FilePath   = i.FilePath,
                UploadedAt = i.UploadedAt
            }).ToList()
        };
    }

    // ── Delete Single Image ───────────────────────────────────────────────────

    public async Task<(bool Success, int EquipmentId, string? Error)> DeleteImageAsync(
        int imageId, string actorUserId)
    {
        var image = await db.EquipmentImages.FindAsync(imageId);
        if (image == null) return (false, 0, "图片不存在");

        var equipmentId = image.EquipmentId;
        db.EquipmentImages.Remove(image);
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(actorUserId, "DeleteEquipmentImage",
            equipmentId.ToString(), $"ImageId={imageId}");

        return (true, equipmentId, null);
    }

    // ── Excel Export ──────────────────────────────────────────────────────────

    public async Task<byte[]> ExportToExcelAsync(
        int? categoryId, EquipmentStatus? status, string? keyword)
    {
        var list = await BuildFilterQuery(categoryId, status, keyword)
            .Include(e => e.Category)
            .Include(e => e.CreatedBy)
            .OrderBy(e => e.EquipmentNo)
            .ToListAsync();

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("设备台账");

        string[] headers =
        [
            "设备编号", "设备名称", "设备分类", "品牌/型号",
            "出厂日期", "出厂编号", "技术参数", "购置日期",
            "原值(元)", "所属单位", "状态", "录入时间", "录入人"
        ];

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
            ws.Cells[1, c + 1].Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var e in list)
        {
            ws.Cells[row, 1].Value  = e.EquipmentNo;
            ws.Cells[row, 2].Value  = e.Name;
            ws.Cells[row, 3].Value  = e.Category.Name;
            ws.Cells[row, 4].Value  = e.BrandModel;
            ws.Cells[row, 5].Value  = e.ManufactureDate.ToString("yyyy-MM-dd");
            ws.Cells[row, 6].Value  = e.FactoryNo;
            ws.Cells[row, 7].Value  = e.TechSpecs;
            ws.Cells[row, 8].Value  = e.PurchaseDate?.ToString("yyyy-MM-dd");
            ws.Cells[row, 9].Value  = e.OriginalValue;
            ws.Cells[row, 9].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 10].Value = e.OwnedBy;
            ws.Cells[row, 11].Value = StatusToString(e.Status);
            ws.Cells[row, 12].Value = e.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            ws.Cells[row, 13].Value = e.CreatedBy.RealName;
            row++;
        }

        if (ws.Dimension != null)
            ws.Cells[ws.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }
}
