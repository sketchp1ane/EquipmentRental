namespace EquipmentRental.Services;

public class FileService(IWebHostEnvironment env)
{
    private readonly IWebHostEnvironment _env = env;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf"];

    private static readonly Dictionary<string, string> AllowedMimeTypes = new()
    {
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png",  "image/png"  },
        { ".pdf",  "application/pdf" }
    };

    // Leading bytes (magic numbers) that must match the file header
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        { ".jpg",  [0xFF, 0xD8, 0xFF] },
        { ".jpeg", [0xFF, 0xD8, 0xFF] },
        { ".png",  [0x89, 0x50, 0x4E, 0x47] },
        { ".pdf",  [0x25, 0x50, 0x44, 0x46] }   // %PDF
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
    {
        // 1. Extension whitelist
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"不允许上传 {ext} 类型的文件。");

        // 2. File size limit
        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("文件大小不能超过 10 MB。");

        // 3. MIME type whitelist (based on Content-Type header)
        if (!AllowedMimeTypes.TryGetValue(ext, out var expectedMime) ||
            !string.Equals(file.ContentType, expectedMime, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("文件 MIME 类型不合法。");

        // 4. Magic bytes — read file header to confirm actual file type
        var magic = MagicBytes[ext];
        using (var headerStream = file.OpenReadStream())
        {
            var header = new byte[magic.Length];
            var read   = await headerStream.ReadAsync(header.AsMemory(0, magic.Length));
            if (read < magic.Length || !header.SequenceEqual(magic))
                throw new InvalidOperationException("文件内容与扩展名不符。");
        }

        // 5. Save with a random GUID filename (no path traversal risk)
        var uploadRoot = Path.Combine(_env.ContentRootPath, "Uploads", subfolder);
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Path.Combine("Uploads", subfolder, fileName);
    }
}
