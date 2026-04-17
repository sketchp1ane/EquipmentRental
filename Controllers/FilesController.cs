using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace EquipmentRental.Controllers;

/// <summary>
/// 提供对 Uploads/ 目录中文件的受鉴权访问。
/// 文件存储在 wwwroot 之外，不能通过静态文件中间件直接访问。
/// </summary>
[Authorize]
public class FilesController(IWebHostEnvironment env) : Controller
{
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    [HttpGet("files/{*filePath}")]
    public IActionResult Get(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return NotFound();

        // 规范化路径分隔符，防止双斜杠等异常输入
        filePath = filePath.Replace('\\', '/').Trim('/');

        // 拼接物理路径
        var fullPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, filePath));

        // 路径越界防护：确保解析后的路径仍在 Uploads/ 目录内
        var uploadsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Uploads"));
        if (!fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(fullPath, contentType);
    }
}
