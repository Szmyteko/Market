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

   
    Task DeleteUserDocsAsync(string userId, CancellationToken ct);
}
