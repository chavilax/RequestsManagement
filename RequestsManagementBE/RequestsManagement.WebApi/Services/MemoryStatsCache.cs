using Microsoft.Extensions.Caching.Memory;
using RequestsManagement.BL.Interfaces;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.WebApi.Services
{
    /// <summary>
    /// מימוש Cache מבוסס IMemoryCache (זיכרון התהליך).
    /// מתאים לדרישה "ברמה מוגבלת". למעבר לסביבת multi-instance יש להחליף ל-Redis
    /// (IDistributedCache) - ראה תיעוד.
    /// </summary>
    public class MemoryStatsCache : IStatsCache
    {
        private const string CacheKey = "requests_stats";
        private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(2);

        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryStatsCache> _logger;

        public MemoryStatsCache(IMemoryCache cache, ILogger<MemoryStatsCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public RequestStatsDTO? Get()
        {
            if (_cache.TryGetValue(CacheKey, out RequestStatsDTO? stats))
            {
                _logger.LogDebug("Stats cache hit");
                return stats;
            }

            _logger.LogDebug("Stats cache miss");
            return null;
        }

        public void Set(RequestStatsDTO stats)
        {
            var options = new MemoryCacheEntryOptions
            {
                // Absolute expiration - פג כעבור פרק זמן קבוע מרגע השמירה
                AbsoluteExpirationRelativeToNow = Expiration
            };

            _cache.Set(CacheKey, stats, options);
            _logger.LogDebug("Stats cache set (expires in {Minutes} min)", Expiration.TotalMinutes);
        }

        public void Invalidate()
        {
            _cache.Remove(CacheKey);
            _logger.LogDebug("Stats cache invalidated");
        }
    }
}
