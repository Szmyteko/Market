using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Market.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    public LocalFileStorage(IWebHostEnvironment env) => _env = env;

    private string UserBaseDir(string userId) => Path.Combine(_env.ContentRootPath, "PrivateStorage", "ids", userId);

    public async Task<string> SaveUserDocAsync(string userId, IFormFile file, string label, CancellationToken ct)
    {
        var baseDir = UserBaseDir(userId);
        Directory.CreateDirectory(baseDir);
        var ext = Path.GetExtension(file.FileName);
        var name = $"{label}-{Guid.NewGuid()}{ext}";
        var full = Path.Combine(baseDir, name);
        await using var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
        await file.CopyToAsync(fs, ct);
        return full;
    }

    public Task<Stream> OpenAsync(string path, CancellationToken ct) =>
        Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));

    public bool Exists(string path) => File.Exists(path);

    public Task DeleteUserDocsAsync(string userId, CancellationToken ct)
    {
       
        ct.ThrowIfCancellationRequested();

        var dir = UserBaseDir(userId);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }
}
