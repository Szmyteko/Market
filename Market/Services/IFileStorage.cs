using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Market.Services;

public interface IFileStorage
{
    Task<string> SaveUserDocAsync(string userId, IFormFile file, string label, CancellationToken ct);
    Task<Stream> OpenAsync(string path, CancellationToken ct);
    bool Exists(string path);

    /// <summary>
    /// Usuwa wszystkie pliki prywatne u¿ytkownika (np. dokumenty do weryfikacji).
    /// </summary>
    Task DeleteUserDocsAsync(string userId, CancellationToken ct);
}
