// Author: Jacques Murray
namespace TieredCache
{
    /// <summary>
    /// Provided a simple interface for interacting with a multi-layer cache (L1/L2).
    /// Encapsulates the logic of checking L1 (in-memory), then L2 (distributed),
    /// and finally calling a factory method to retrieve the source data.
    /// </summary>
    public interface ITieredCache
    {
        /// <summary>
        /// Gets an item from the cache.
        /// Checks L1 (Memory) -> L2 (Distributed).
        /// If not found in any tier, returns default(T).
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve.</typeparam>
        /// <param name="key">The unique key for the item.</param>
        /// <returns>The cached item or default(T) if not found.</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Gets an item from the cache or creates it using the provided factory.
        /// 1. Check L1 (Memory).
        /// 2. If L1 miss, check L2 (Distributed).
        /// 3. If L2 hit, deserialize, store in L1, and return.
        /// 4. If L2 miss, execute factory, store result in L2 and L2, and return.
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve.</typeparam>
        /// <param name="key">The unique key for the item.</param>
        /// <param name="factory">The asynchronous factory method to create the item if not found.</param>
        /// <param name="options">The cache expiration policies for L1 and L2 tiers.</param>
        /// <returns>The cached or newly created item.</returns>
        Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TieredCacheEntryOptions options);

        /// <summary>
        /// Manually sets an item in all cache tiers (L1 and L2).
        /// </summary>
        /// <typeparam name="T">The type of the item to set.</typeparam>
        /// <param name="key">The unique key for the item.</param>
        /// <param name="value">The item to store.</param>
        /// <param name="options">The cache expiration policies for L1 and L2 tiers.</param>
        Task SetAsync<T>(
            string key,
            T value,
            TieredCacheEntryOptions options);

        /// <summary>
        /// Removes an item from 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(string key);
    }
}
