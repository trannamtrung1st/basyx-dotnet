using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaSyx.Aas.Server.Services.Abstracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tmds.Linux;

namespace BaSyx.Aas.Server.Services
{
    public class CachedFileService : ICachedFileService
    {
        public static ICachedFileService Global { get; set; }

        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedFileService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public CachedFileService(IMemoryCache cache, ILogger<CachedFileService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetFileContentsAsync(string filePath, TimeSpan? cacheExpiration = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var semaphore = _locks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync();

                if (_cache.TryGetValue(filePath, out string cachedContent))
                {
                    _logger.LogDebug($"Cache hit for file: {filePath}");
                    return cachedContent;
                }

                _logger.LogDebug($"Cache miss for file: {filePath}");

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found: {filePath}");
                    throw new FileNotFoundException($"The file {filePath} was not found.");
                }

                string content = await File.ReadAllTextAsync(filePath);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSize(1) // Size can be adjusted based on your requirements
                    .SetPriority(CacheItemPriority.High);

                if (cacheExpiration.HasValue)
                    cacheEntryOptions.SetAbsoluteExpiration(cacheExpiration.Value);

                _cache.Set(filePath, content, cacheEntryOptions);

                return content;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void InvalidateCache(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            _cache.Remove(filePath);
            _logger.LogDebug($"Cache invalidated for file: {filePath}");
        }
    }
}

