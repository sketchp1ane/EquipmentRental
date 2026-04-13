namespace EquipmentRental.Services;

public class FileService(IWebHostEnvironment env)
{
    private readonly IWebHostEnvironment _env = env;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf"];

    public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"不允许上传 {ext} 类型的文件。");

        var uploadRoot = Path.Combine(_env.ContentRootPath, "Uploads", subfolder);
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Path.Combine("Uploads", subfolder, fileName);
    }
}
