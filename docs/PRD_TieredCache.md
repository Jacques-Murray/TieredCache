# **Product Requirements Document: TieredCache**

* **Document Version:** 1.0  
* **Date:** 2025-11-13  
* **Owner:** Jacques

---

## **1. Introduction**

**TieredCache** is a .NET library designed to abstract and simplify multi-layer caching.

In modern .NET applications, developers frequently combine a local, in-memory cache (L1, using IMemoryCache) for speed and a shared, distributed cache (L2, like Redis, using IDistributedCache) for scalability.

This forces developers to write repetitive, boilerplate logic in their services:

1. Check L1 for an item.  
2. If L1 miss, check L2.  
3. If L2 hit, deserialize item, store in L1, and return.  
4. If L2 miss, call the data source (e.g., database).  
5. Take the result, serialize it, store in L2.  
6. Store the result in L1.  
7. Return the result.

This logic is error-prone, clutters business code, and is hard to maintain.

**TieredCache** solves this by providing a single interface, ITieredCache, that encapsulates this entire workflow. Developers can register their L1 and L2 providers and then make a single GetOrCreateAsync call.

---

## **2. Target Audience & Use Cases**

* **Audience:** .NET developers building Web APIs, microservices, or any application that needs to read data with high performance.  
* **Use Case 1 (Read-Heavy API):** A service endpoint needs to fetch product details. This data is read 1000x more often than it is written. The developer wants to serve requests from in-memory (L1) if possible, fall back to a shared Redis cache (L2), and only hit the database as a last resort.  
* **Use Case 2 (Scalability):** A service is scaled to 10 instances. When one instance fetches data and populates the L2 cache, the other 9 instances should benefit. When any instance accesses that data, it should be promoted to its local L1 cache for subsequent fast access.

---

## **3. Goals & Objectives**

* **Reduce Boilerplate:** Eliminate 90% of the manual cache-checking logic from application services.  
* **Improve Readability:** Make data retrieval logic clean and focused on the business intent, not the caching mechanics.  
* **Ease of Use:** Provide a simple, fluent registration API in Program.cs that uses existing DI-registered caches.  
* **Flexibility:** Allow developers to specify *different* expiration policies for L1 and L2 for the same item.

---

## **4. Functional Requirements**

### **FR4.1: Core Interface**

The library must provide a primary interface for interaction.

```C#
public interface ITieredCache  
{  
    // The primary method for data retrieval  
    Task\<T\> GetOrCreateAsync\<T\>(  
        string key,  
        Func\<Task\<T\>\> factory,  
        TieredCacheEntryOptions options);

    // Manual removal from all tiers  
    Task RemoveAsync(string key);

    // Manual setting of a value in all tiers  
    Task SetAsync\<T\>(  
        string key,  
        T value,  
        TieredCacheEntryOptions options);

    // Get-only (no factory), returns default(T) if not found  
    Task\<T\> GetAsync\<T\>(string key);  
}
```

### **FR4.2: Tiered Options Class**

A new options class is required to define separate policies for L1 and L2.

```C#
public class TieredCacheEntryOptions  
{  
    // Options for the IMemoryCache (L1)  
    public MemoryCacheEntryOptions L1Options { get; set; }

    // Options for the IDistributedCache (L2)  
    public DistributedCacheEntryOptions L2Options { get; set; }  
}
```

### **FR4.3: Dependency Injection**

The library must be configurable via IServiceCollection extension methods.

* services.AddTieredCache();  
* This method should automatically find and use the already-registered IMemoryCache and IDistributedCache from the service container.  
* It must register ITieredCache as a Scoped or Transient service.

### **FR4.4: Pluggable Serialization**

IDistributedCache stores byte\[\], so serialization is required.

* The library must define a simple ITieredCacheSerializer interface.  
* The library must provide a default implementation using System.Text.Json (JsonTieredCacheSerializer).  
* The DI registration must allow overriding the serializer: services.AddTieredCache().WithSerializer\<MyCustomSerializer\>();

### **FR4.5: Core GetOrCreateAsync Logic**

This is the core business logic of the library. The flow must be:

1. Generate an internal cache key.  
2. **Check L1 (IMemoryCache):**  
   * \_memoryCache.TryGetValue(key, out T item)  
   * If **hit**, return item.  
3. **Check L2 (IDistributedCache):**  
   * \_distributedCache.GetAsync(key)  
   * If **hit**:  
     * Call \_serializer.Deserialize\<T\>(bytes).  
     * Store the deserialized item in L1: \_memoryCache.Set(key, item, options.L1Options).  
     * Return item.  
4. **Cache Miss (Factory Execution):**  
   * Execute the user's factory() method: T item \= await factory();  
   * If item is not null:  
     * **Set L2:** Serialize item (\_serializer.Serialize(item)) and call \_distributedCache.SetAsync(key, bytes, options.L2Options).  
     * **Set L1:** Call \_memoryCache.Set(key, item, options.L1Options).  
   * Return item.

### **FR4.6: RemoveAsync Logic**

* \_memoryCache.Remove(key)  
* \_distributedCache.RemoveAsync(key)  
* Both tiers must be cleared.

---

## **5. Non-Functional Requirements**

* **Performance:** The overhead of the library's logic (checks and DI) must be negligible. The primary performance cost will be serialization, which is unavoidable.  
* **Reliability:** If the L2 cache (e.g., Redis) is unavailable, the library must *not* throw an exception that breaks the application. It should log the error and (ideally) fall back to behaving as an L1-only cache.  
* **Thread Safety:** The GetOrCreateAsync method must be thread-safe.  
* **Documentation:** The NuGet package must have a detailed README.md explaining:  
  * Setup in Program.cs.  
  * Usage in a service.  
  * How to configure L1/L2 options.  
  * How to provide a custom serializer.

---

## **6. Out of Scope (v1.0)**

* **Automatic Cache Invalidation Bus:** Automatically removing an L1 item from *all* app instances when an L2 item is updated is complex. This feature (requiring a message bus like Redis Pub/Sub) is out of scope for v1.0. RemoveAsync will only clear the L1 cache on the instance that calls it.  
* **More than 2 Tiers:** v1.0 will only support the L1 (Memory) and L2 (Distributed) pattern.  
* **Thundering Herd Protection:** Advanced SemaphoreSlim logic to prevent multiple threads from executing the factory simultaneously for the same key is a v2.0 feature.

---

## **7. API Design & Code Examples**

This is how a developer will use the library.

### **Program.cs**

```C#
var builder \= WebApplication.CreateBuilder(args);

// 1. Add standard .NET caches  
builder.Services.AddMemoryCache();  
builder.Services.AddStackExchangeRedisCache(options \=\>  
{  
    options.Configuration \= builder.Configuration.GetConnectionString("Redis");  
});

// 2. Add TieredCache  
// This automatically finds the services above.  
builder.Services.AddTieredCache(); 

// ... rest of setup
```

### **MyProductService.cs**

```C#
public class ProductService  
{  
    private readonly ITieredCache \_cache;  
    private readonly AppDbContext \_db;

    public ProductService(ITieredCache cache, AppDbContext db)  
    {  
        \_cache \= cache;  
        \_db \= db;  
    }

    public async Task\<Product\> GetProductByIdAsync(int id)  
    {  
        string key \= $"product:{id}";

        var options \= new TieredCacheEntryOptions  
        {  
            // Keep in local memory for 5 minutes  
            L1Options \= new MemoryCacheEntryOptions()  
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)),  
              
            // Keep in shared Redis cache for 1 hour  
            L2Options \= new DistributedCacheEntryOptions()  
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))  
        };

        // This single call handles all the logic  
        return await \_cache.GetOrCreateAsync(key, async () \=\>  
        {  
            // This factory only runs on a total cache miss  
            return await \_db.Products.FindAsync(id);  
        }, options);  
    }

    public async Task ClearProductFromCacheAsync(int id)  
    {  
        // This removes from both L1 and L2  
        await \_cache.RemoveAsync($"product:{id}");  
    }  
}  
```