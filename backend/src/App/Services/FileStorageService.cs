using Microsoft.AspNetCore.StaticFiles;

namespace App.Services;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<(Stream FileStream, string ContentType)?> GetFileAsync(string fileUrl);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    private const string ImageFolder = "images";

    private static readonly string[] AllowedExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("File is empty.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException("File size cannot exceed 5 MB.");
        }

        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Only JPG, JPEG, PNG and WEBP files are allowed.");
        }

        if (string.IsNullOrWhiteSpace(_environment.WebRootPath))
        {
            throw new InvalidOperationException("WebRootPath is not configured.");
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, ImageFolder);

        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var fileStream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None
        );

        await file.CopyToAsync(fileStream);

        return $"/{ImageFolder}/{uniqueFileName}";
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.FromResult(false);
        }

        var relativePath = fileUrl.TrimStart('/');

        var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

        if (!File.Exists(physicalPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(physicalPath);

        return Task.FromResult(true);
    }

    public Task<(Stream FileStream, string ContentType)?> GetFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        var relativePath = fileUrl.TrimStart('/');

        var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

        if (!File.Exists(physicalPath))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        if (!_contentTypeProvider.TryGetContentType(physicalPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        Stream fileStream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
        );

        return Task.FromResult<(Stream, string)?>((fileStream, contentType));
    }
}