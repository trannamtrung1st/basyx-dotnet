using System;
using System.Threading.Tasks;

namespace BaSyx.Aas.Server.Services.Abstracts
{
    public interface ICachedFileService
    {
        Task<string> GetFileContentsAsync(string filePath, TimeSpan? cacheExpiration = null);
        void InvalidateCache(string filePath);
    }
}

