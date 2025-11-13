// Author: Jacques Murray
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace TieredCache
{
    /// <summary>
    /// Provides tiered cache entry options, allowing for different
    /// expiration and priority policies for L1 (Memory) and L2 (Distributed) caches.
    /// </summary>
    public class TieredCacheEntryOptions
    {
        /// <summary>
        /// Gets or sets the options for the L1 (IMemoryCache) cache entry.
        /// </summary>
        public MemoryCacheEntryOptions? L1Options { get; set; }

        /// <summary>
        /// Gets or sets the options for the L2 (IDistributedCache) cache entry.
        /// </summary>
        public DistributedCacheEntryOptions? L2Options { get; set; }
    }
}
