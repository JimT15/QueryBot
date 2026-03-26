using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QueryBot.Configuration;

namespace QueryBot.Pages;

[Authorize]
[RequestSizeLimit(10 * 3 * 1024 * 1024 + 65536)]
public sealed class DashboardModel : PageModel
{
    private const long MaxFileSizeBytes = 3 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = [".docx", ".xlsx", ".pdf", ".txt"];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly QueryBotSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardModel(IOptions<QueryBotSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public IList<IFormFile>? Uploads { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostUploadAsync(CancellationToken ct)
    {
        if (Uploads is null || Uploads.Count == 0)
        {
            ErrorMessage = "Please select at least one file to upload.";
            return Page();
        }

        foreach (var file in Uploads)
        {
            if (file.Length == 0)
            {
                ErrorMessage = $"\"{Path.GetFileName(file.FileName)}\" is empty.";
                return Page();
            }

            if (file.Length > MaxFileSizeBytes)
            {
                ErrorMessage = $"\"{Path.GetFileName(file.FileName)}\" exceeds the 3 MB maximum size.";
                return Page();
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                ErrorMessage = $"\"{Path.GetFileName(file.FileName)}\" is not an accepted file type.";
                return Page();
            }
        }

        var clientIdClaim = User.FindFirst("ClientId")?.Value;
        if (!long.TryParse(clientIdClaim, out var clientId) || clientId <= 0)
        {
            ErrorMessage = "Your session does not include a valid client identity. Please log out and log in again.";
            return Page();
        }

        var storagePath = _settings.AttachmentStoragePath;
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            ErrorMessage = "Upload storage is not configured. Please contact support.";
            return Page();
        }

        Directory.CreateDirectory(storagePath);

        var savedFiles = new List<(string SavedFilePath, string OriginalFileName, string ContentType, long FileSizeBytes, DateTime UploadedUtc)>();

        foreach (var file in Uploads)
        {
            var uploadedUtc = DateTime.UtcNow;
            var originalFileName = Path.GetFileName(file.FileName);
            var ext = Path.GetExtension(originalFileName);
            var timestamp = uploadedUtc.ToString("yyyyMMddHHmmssfff");
            var guidSuffix = Guid.NewGuid().ToString("N")[..8];
            var savedFileName = $"{clientId}_{timestamp}_{guidSuffix}{ext}";
            var savedFilePath = Path.Combine(storagePath, savedFileName);

            await using (var stream = System.IO.File.Create(savedFilePath))
            {
                await file.CopyToAsync(stream, ct);
            }

            savedFiles.Add((savedFilePath, originalFileName, file.ContentType ?? string.Empty, file.Length, uploadedUtc));
        }

        var documents = savedFiles.Select(f => new
        {
            savedFilePath = f.SavedFilePath,
            originalFileName = f.OriginalFileName,
            contentType = f.ContentType,
            fileSizeBytes = f.FileSizeBytes,
            uploadedUtc = f.UploadedUtc
        }).ToList();

        try
        {
            var http = _httpClientFactory.CreateClient("QuexPlatform");
            var response = await http.PostAsJsonAsync(
                "querybot/train-ai",
                new { clientId, documents },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                foreach (var (savedFilePath, _, _, _, _) in savedFiles)
                    System.IO.File.Delete(savedFilePath);
                ErrorMessage = "The files were received but could not be queued for processing. Please try again.";
                return Page();
            }
        }
        catch
        {
            foreach (var (savedFilePath, _, _, _, _) in savedFiles)
                System.IO.File.Delete(savedFilePath);
            ErrorMessage = "The files were received but could not be queued for processing. Please try again.";
            return Page();
        }

        SuccessMessage = savedFiles.Count == 1
            ? $"\"{savedFiles[0].OriginalFileName}\" uploaded successfully."
            : $"{savedFiles.Count} files uploaded successfully.";
        return Page();
    }
}
