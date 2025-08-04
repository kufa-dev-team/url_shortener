using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
// ADVANCED C# GEMS: Add these using statements for advanced features
// using System.Linq.Expressions; // For dynamic LINQ expressions
// using System.Runtime.CompilerServices; // For CallerMemberName, ConfigureAwait
// using Microsoft.Extensions.Caching.Memory; // For in-memory caching
// using System.Collections.Concurrent; // For thread-safe collections

// REDIS DISTRIBUTED CACHING: Add these for Redis integration
// using StackExchange.Redis; // Main Redis client
// using Microsoft.Extensions.Caching.Distributed; // IDistributedCache interface
// using Microsoft.Extensions.Caching.StackExchangeRedis; // Redis implementation
// using System.Text.Json; // For JSON serialization to Redis

namespace Infrastructure.Repositories;

// ADVANCED C# GEM: Generic constraints for better type safety
// public class Repository<T> : IRepository<T> where T : BaseEntity, new()
// ADVANCED C# GEM: Multiple constraints
// public class Repository<T> : IRepository<T> where T : class, IEntity, new()
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;
    
    // ADVANCED C# GEM: Lazy initialization for expensive operations
    // private readonly Lazy<IMemoryCache> _cache = new(() => new MemoryCache(new MemoryCacheOptions()));
    
    // REDIS CACHING: Distributed cache for scalable applications
    // private readonly IDistributedCache _distributedCache;
    // private readonly IDatabase _redisDatabase; // Direct Redis access for advanced operations
    
    // ADVANCED C# GEM: Static readonly for compile-time constants
    // private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromMinutes(5);
    
    // REDIS CACHING: Different expiry times for different data types
    // private static readonly TimeSpan ShortCacheExpiry = TimeSpan.FromMinutes(5);   // Frequently changing data
    // private static readonly TimeSpan MediumCacheExpiry = TimeSpan.FromHours(1);    // Moderately stable data
    // private static readonly TimeSpan LongCacheExpiry = TimeSpan.FromDays(1);       // Rarely changing data

    public Repository(ApplicationDbContext context)
    {
        // ADVANCED C# GEM: Null-coalescing assignment (C# 8.0)
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
        
        // REDIS CACHING: Constructor with distributed cache injection
        // public Repository(ApplicationDbContext context, IDistributedCache distributedCache, IConnectionMultiplexer redis)
        // {
        //     _context = context ?? throw new ArgumentNullException(nameof(context));
        //     _dbSet = context.Set<T>();
        //     _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        //     _redisDatabase = redis?.GetDatabase() ?? throw new ArgumentNullException(nameof(redis));
        // }
        
        // ADVANCED C# GEM: Pattern matching with switch expressions (C# 8.0)
        // var cacheKey = typeof(T).Name switch
        // {
        //     nameof(User) => "users",
        //     nameof(Product) => "products",
        //     _ => typeof(T).Name.ToLowerInvariant()
        // };
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        // ADVANCED C# GEM: Guard clauses with throw expressions (C# 7.0)
        // ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        
        // ADVANCED C# GEM: ConfigureAwait(false) for library code
        // return await _dbSet.FindAsync(id).ConfigureAwait(false);
        
        // ADVANCED C# GEM: Null-conditional with async (C# 8.0)
        // return await _dbSet.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        
        // ADVANCED C# GEM: Using cancellation tokens
        // public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        // return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        
        // REDIS CACHING PATTERNS: Different strategies for data consistency and performance
        
        // 1. CACHE-ASIDE PATTERN (Lazy Loading) - Most common, app manages cache
        // var cacheKey = $"{typeof(T).Name}_{id}";
        // var cachedJson = await _distributedCache.GetStringAsync(cacheKey);
        // if (cachedJson != null)
        // {
        //     return JsonSerializer.Deserialize<T>(cachedJson);
        // }
        // var entity = await _dbSet.FindAsync(id);
        // if (entity != null)
        // {
        //     var serialized = JsonSerializer.Serialize(entity);
        //     await _distributedCache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        //     {
        //         AbsoluteExpirationRelativeToNow = DefaultCacheExpiry
        //     });
        // }
        // return entity;
        
        // 2. READ-THROUGH PATTERN - Cache automatically loads data on cache miss
        // PROS: Transparent to application, cache handles loading logic
        // CONS: More complex cache implementation, potential for cache stampede
        // Implementation: Cache layer intercepts read requests and loads from DB if not cached
        // 
        // Example with custom cache wrapper:
        // return await _cacheWrapper.GetAsync(cacheKey, async () => 
        // {
        //     return await _dbSet.FindAsync(id); // This only executes on cache miss
        // }, DefaultCacheExpiry);
        //
        // READ-THROUGH with Redis Lua script to prevent race conditions:
        // const string luaScript = @"
        //     local cached = redis.call('GET', KEYS[1])
        //     if cached then
        //         return cached
        //     else
        //         redis.call('SETEX', KEYS[1], ARGV[2], ARGV[1])
        //         return ARGV[1]
        //     end";
        // var result = await _redisDatabase.ScriptEvaluateAsync(luaScript, new RedisKey[] { cacheKey }, 
        //                                                       new RedisValue[] { serializedEntity, (int)DefaultCacheExpiry.TotalSeconds });
        
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        // BASIC LINQ: Filtering and sorting
        // return await _dbSet.Where(x => x.IsActive).OrderBy(x => x.CreatedAt).ToListAsync();
        
        // ADVANCED LINQ: Complex filtering with multiple conditions
        // return await _dbSet.Where(x => x.CreatedAt > DateTime.UtcNow.AddDays(-30) && x.IsActive)
        //                   .OrderByDescending(x => x.UpdatedAt)
        //                   .ThenBy(x => x.Id)
        //                   .ToListAsync();
        
        // PAGINATION HINT: Skip and Take for paging
        // return await _dbSet.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToListAsync();
        
        // PERFORMANCE: Use AsNoTracking() for read-only data
        // return await _dbSet.AsNoTracking().ToListAsync();
        return await _dbSet.ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        // ADVANCED C# GEM: Null-coalescing assignment with throw expression
        ArgumentNullException.ThrowIfNull(entity);
        
        // ADVANCED C# GEM: Using DateTimeOffset for timezone awareness
        // entity.CreatedAt = DateTimeOffset.UtcNow;
        // entity.UpdatedAt = DateTimeOffset.UtcNow;
        
        // ADVANCED C# GEM: Object initializer with conditional assignment
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        
        // ADVANCED C# GEM: Using local functions for validation
        // void ValidateEntity()
        // {
        //     if (string.IsNullOrWhiteSpace(entity.Name))
        //         throw new ArgumentException("Name cannot be empty", nameof(entity));
        // }
        // ValidateEntity();
        
        // ADVANCED C# GEM: Fluent API chaining
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        
        // REDIS CACHING PATTERNS FOR WRITE OPERATIONS:
        
        // 1. WRITE-THROUGH PATTERN - Write to cache and database simultaneously
        // PROS: Data consistency guaranteed, cache always up-to-date
        // CONS: Higher write latency, both cache and DB must succeed
        // 
        // var cacheKey = $"{typeof(T).Name}_{entity.Id}";
        // var serialized = JsonSerializer.Serialize(entity);
        // 
        // // Write to both cache and database in a coordinated manner
        // var cacheTask = _distributedCache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        // {
        //     AbsoluteExpirationRelativeToNow = DefaultCacheExpiry
        // });
        // var dbTask = _context.SaveChangesAsync();
        // 
        // // Wait for both operations to complete
        // await Task.WhenAll(cacheTask, dbTask);
        // 
        // // Alternative: Use Redis transactions for atomicity
        // var transaction = _redisDatabase.CreateTransaction();
        // transaction.StringSetAsync(cacheKey, serialized, DefaultCacheExpiry);
        // // If DB save fails, transaction can be discarded
        
        // 2. WRITE-BEHIND (WRITE-BACK) PATTERN - Write to cache first, DB later
        // PROS: Fastest write performance, can batch DB writes
        // CONS: Risk of data loss if cache fails before DB write
        // 
        // // Write to cache immediately
        // await _distributedCache.SetStringAsync(cacheKey, serialized);
        // 
        // // Queue DB write for later (using background service)
        // await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async token =>
        // {
        //     await _context.SaveChangesAsync(token);
        // });
        
        // 3. CACHE INVALIDATION STRATEGIES:
        // 
        // Simple key invalidation:
        // await _distributedCache.RemoveAsync($"{typeof(T).Name}_{entity.Id}");
        // 
        // Pattern-based invalidation using Redis:
        // var pattern = $"{typeof(T).Name}_*";
        // var server = _redis.GetServer(_redis.GetEndPoints().First());
        // var keys = server.Keys(pattern: pattern);
        // await _redisDatabase.KeyDeleteAsync(keys.ToArray());
        // 
        // Tag-based invalidation (Redis 6.2+):
        // await _redisDatabase.StringSetAsync(cacheKey, serialized, DefaultCacheExpiry, 
        //                                   flags: CommandFlags.None);
        // await _redisDatabase.SetAddAsync($"tags:{typeof(T).Name}", cacheKey);
        // 
        // Cache warming after invalidation:
        // await InvalidateCacheAsync(entity.Id);
        // _ = Task.Run(async () => await WarmCacheAsync(entity.Id)); // Fire and forget
        
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        // ADVANCED C# GEM: Using records for immutable updates (C# 9.0)
        // entity = entity with { UpdatedAt = DateTime.UtcNow };
        
        // ADVANCED C# GEM: Optimistic concurrency with timestamp checking
        // var existing = await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Id);
        // if (existing?.Timestamp != entity.Timestamp)
        //     throw new DbUpdateConcurrencyException("Entity was modified by another user");
        
        entity.UpdatedAt = DateTime.UtcNow;
        
        // ADVANCED C# GEM: Selective property updates using Entry
        // _context.Entry(entity).Property(x => x.UpdatedAt).IsModified = true;
        // _context.Entry(entity).Property(x => x.Name).IsModified = true;
        
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        
        // REDIS CACHING FOR UPDATE OPERATIONS:
        
        // WRITE-THROUGH UPDATE PATTERN:
        // 1. Update database first (for consistency)
        // 2. Update cache with new data
        // 3. Handle partial failures gracefully
        // 
        // var cacheKey = $"{typeof(T).Name}_{entity.Id}";
        // 
        // try
        // {
        //     // Database update first
        //     await _context.SaveChangesAsync();
        //     
        //     // Cache update second
        //     var serialized = JsonSerializer.Serialize(entity);
        //     await _distributedCache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        //     {
        //         AbsoluteExpirationRelativeToNow = DefaultCacheExpiry
        //     });
        // }
        // catch (Exception)
        // {
        //     // If cache update fails, invalidate to prevent stale data
        //     await _distributedCache.RemoveAsync(cacheKey);
        //     throw;
        // }
        
        // OPTIMISTIC LOCKING WITH REDIS:
        // Use Redis for distributed locking to prevent concurrent updates
        // 
        // var lockKey = $"lock:{typeof(T).Name}_{entity.Id}";
        // var lockValue = Guid.NewGuid().ToString();
        // var lockAcquired = await _redisDatabase.StringSetAsync(lockKey, lockValue, 
        //                                                       TimeSpan.FromSeconds(30), When.NotExists);
        // 
        // if (!lockAcquired)
        //     throw new InvalidOperationException("Entity is being modified by another process");
        // 
        // try
        // {
        //     // Perform update operations
        //     await _context.SaveChangesAsync();
        //     await UpdateCacheAsync(entity);
        // }
        // finally
        // {
        //     // Release lock only if we own it
        //     const string releaseLockScript = @"
        //         if redis.call('GET', KEYS[1]) == ARGV[1] then
        //             return redis.call('DEL', KEYS[1])
        //         else
        //             return 0
        //         end";
        //     await _redisDatabase.ScriptEvaluateAsync(releaseLockScript, 
        //                                            new RedisKey[] { lockKey }, 
        //                                            new RedisValue[] { lockValue });
        // }
        
        // ADVANCED C# GEM: Event sourcing pattern
        // await PublishDomainEvent(new EntityUpdatedEvent<T>(entity));
    }

    public async Task DeleteAsync(int id)
    {
        // Good use of LINQ! Consider adding validation:
        // var exists = await _dbSet.AnyAsync(e => e.Id == id);
        // if (!exists) throw new EntityNotFoundException();
        
        // REDIS CACHING FOR DELETE OPERATIONS:
        
        // WRITE-THROUGH DELETE PATTERN:
        // 1. Delete from database first
        // 2. Remove from cache
        // 3. Handle cache invalidation for related data
        // 
        // var cacheKey = $"{typeof(T).Name}_{id}";
        // 
        // try
        // {
        //     // Database deletion first
        //     await _dbSet.Where(e => e.Id == id).ExecuteDeleteAsync();
        //     
        //     // Cache removal second
        //     await _distributedCache.RemoveAsync(cacheKey);
        //     
        //     // Invalidate related caches (lists, aggregations, etc.)
        //     await InvalidateRelatedCachesAsync(id);
        // }
        // catch (Exception)
        // {
        //     // If cache removal fails, log but don't fail the operation
        //     // Stale cache entries will expire naturally
        //     // _logger.LogWarning("Failed to remove cache entry for {EntityType} with ID {Id}", typeof(T).Name, id);
        //     throw;
        // }
        
        // BULK CACHE INVALIDATION for related data:
        // 
        // private async Task InvalidateRelatedCachesAsync(int deletedId)
        // {
        //     var tasks = new List<Task>
        //     {
        //         // Remove from list caches
        //         _distributedCache.RemoveAsync($"{typeof(T).Name}_list"),
        //         _distributedCache.RemoveAsync($"{typeof(T).Name}_active_list"),
        //         
        //         // Remove from search result caches
        //         InvalidateSearchCachesAsync(),
        //         
        //         // Remove from aggregation caches
        //         _distributedCache.RemoveAsync($"{typeof(T).Name}_count"),
        //         _distributedCache.RemoveAsync($"{typeof(T).Name}_stats")
        //     };
        //     
        //     await Task.WhenAll(tasks);
        // }
        
        await _dbSet.Where(e => e.Id == id).ExecuteDeleteAsync();
    }

    // ADVANCED C# GEMS: Extension methods you might want to add:
    
    // ADVANCED C# GEM: Generic method with multiple constraints
    // public async Task<TResult> ProjectAsync<TResult>(Expression<Func<T, TResult>> selector)
    //     where TResult : class
    // {
    //     return await _dbSet.Select(selector).FirstOrDefaultAsync();
    // }
    
    // ADVANCED C# GEM: Async enumerable (C# 8.0)
    // public async IAsyncEnumerable<T> GetAllStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    // {
    //     await foreach (var entity in _dbSet.AsAsyncEnumerable().WithCancellation(cancellationToken))
    //     {
    //         yield return entity;
    //     }
    // }
    
    // ADVANCED C# GEM: Pattern matching with when clauses
    // public async Task<T?> FindByConditionAsync(object criteria) => criteria switch
    // {
    //     int id when id > 0 => await GetByIdAsync(id),
    //     string name when !string.IsNullOrEmpty(name) => await _dbSet.FirstOrDefaultAsync(x => x.Name == name),
    //     _ => null
    // };
    
    // ADVANCED C# GEM: Span<T> for high-performance scenarios
    // public async Task<T[]> GetByIdsAsync(ReadOnlySpan<int> ids)
    // {
    //     var idList = ids.ToArray(); // Convert span to array for EF Core
    //     return await _dbSet.Where(x => idList.Contains(x.Id)).ToArrayAsync();
    // }
    
    // ADVANCED C# GEM: ValueTask for frequently synchronous operations
    // public ValueTask<bool> ExistsAsync(int id)
    // {
    //     // If cached, return synchronously
    //     if (_cache.Value.TryGetValue($"exists_{id}", out bool exists))
    //         return ValueTask.FromResult(exists);
    //     
    //     return new ValueTask<bool>(CheckExistsAsync(id));
    // }
    
    // ADVANCED C# GEM: Using init-only properties and records
    // public record EntityFilter
    // {
    //     public string? Name { get; init; }
    //     public DateTime? CreatedAfter { get; init; }
    //     public bool? IsActive { get; init; }
    // }
    
    // ADVANCED C# GEM: Builder pattern with fluent interface
    // public class QueryBuilder<T>
    // {
    //     private IQueryable<T> _query;
    //     public QueryBuilder(IQueryable<T> query) => _query = query;
    //     public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate) { _query = _query.Where(predicate); return this; }
    //     public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector) { _query = _query.OrderBy(keySelector); return this; }
    //     public Task<List<T>> ToListAsync() => _query.ToListAsync();
    // }
    
    // ==================================================================================
    // COMPREHENSIVE REDIS CACHING PATTERNS COMPARISON
    // ==================================================================================
    
    // 1. CACHE-ASIDE (LAZY LOADING) PATTERN:
    // ┌─────────────┐    Cache Miss    ┌─────────────┐    ┌──────────────┐
    // │ Application │ ──────────────► │    Cache    │    │   Database   │
    // │             │                 │             │    │              │
    // │             │ ◄────────────── │             │    │              │
    // │             │    No Data      │             │    │              │
    // │             │                 │             │    │              │
    // │             │ ──────────────────────────────────► │              │
    // │             │           Load Data                 │              │
    // │             │ ◄────────────────────────────────── │              │
    // │             │           Return Data               │              │
    // │             │                 │             │    │              │
    // │             │ ──────────────► │             │    │              │
    // │             │   Store Data    │             │    │              │
    // └─────────────┘                 └─────────────┘    └──────────────┘
    //
    // PROS: Simple, application controls caching logic, cache failures don't affect reads
    // CONS: Cache miss penalty, potential for stale data, cache stampede possible
    // USE CASE: Most common pattern, good for read-heavy workloads with acceptable latency
    
    // 2. READ-THROUGH PATTERN:
    // ┌─────────────┐                 ┌─────────────┐    ┌──────────────┐
    // │ Application │ ──────────────► │    Cache    │    │   Database   │
    // │             │    Read Data    │   (Smart)   │    │              │
    // │             │                 │             │    │              │
    // │             │                 │ ──────────► │    │              │
    // │             │                 │ Load on Miss│    │              │
    // │             │                 │ ◄────────── │    │              │
    // │             │                 │             │    │              │
    // │             │ ◄────────────── │             │    │              │
    // │             │   Return Data   │             │    │              │
    // └─────────────┘                 └─────────────┘    └──────────────┘
    //
    // PROS: Transparent to application, cache handles loading, consistent interface
    // CONS: More complex cache implementation, cache becomes critical component
    // USE CASE: When you want to abstract caching logic from application code
    
    // 3. WRITE-THROUGH PATTERN:
    // ┌─────────────┐                 ┌─────────────┐    ┌──────────────┐
    // │ Application │ ──────────────► │    Cache    │    │   Database   │
    // │             │   Write Data    │             │    │              │
    // │             │                 │ ──────────► │    │              │
    // │             │                 │ Write to DB │    │              │
    // │             │                 │ ◄────────── │    │              │
    // │             │                 │   Success   │    │              │
    // │             │ ◄────────────── │             │    │              │
    // │             │   Acknowledge   │             │    │              │
    // └─────────────┘                 └─────────────┘    └──────────────┘
    //
    // PROS: Strong consistency, cache always up-to-date, no stale data
    // CONS: Higher write latency, both cache and DB must be available
    // USE CASE: When data consistency is critical, financial transactions
    
    // 4. WRITE-BEHIND (WRITE-BACK) PATTERN:
    // ┌─────────────┐                 ┌─────────────┐    ┌──────────────┐
    // │ Application │ ──────────────► │    Cache    │    │   Database   │
    // │             │   Write Data    │             │    │              │
    // │             │ ◄────────────── │             │    │              │
    // │             │   Quick ACK     │             │    │              │
    // │             │                 │             │    │              │
    // │             │                 │ ──────────► │    │              │
    // │             │                 │ Async Write │    │              │
    // │             │                 │ (Later)     │    │              │
    // └─────────────┘                 └─────────────┘    └──────────────┘
    //
    // PROS: Fastest write performance, can batch writes, reduces DB load
    // CONS: Risk of data loss, eventual consistency, complex error handling
    // USE CASE: High-throughput writes, analytics data, when some data loss is acceptable
    
    // 5. REFRESH-AHEAD PATTERN:
    // ┌─────────────┐                 ┌─────────────┐    ┌──────────────┐
    // │ Application │ ──────────────► │    Cache    │    │   Database   │
    // │             │    Read Data    │             │    │              │
    // │             │ ◄────────────── │             │    │              │
    // │             │   Return Data   │             │    │              │
    // │             │                 │             │    │              │
    // │             │                 │ ──────────► │    │              │
    // │             │                 │ Refresh     │    │              │
    // │             │                 │ (Background)│    │              │
    // │             │                 │ ◄────────── │    │              │
    // └─────────────┘                 └─────────────┘    └──────────────┘
    //
    // PROS: Low latency reads, proactive cache warming, reduces cache misses
    // CONS: Complex implementation, may refresh unused data, resource overhead
    // USE CASE: Predictable access patterns, when cache misses are expensive
    
    // REDIS-SPECIFIC IMPLEMENTATION PATTERNS:
    
    // A. DISTRIBUTED LOCKING for cache consistency:
    // private async Task<bool> AcquireLockAsync(string lockKey, TimeSpan expiry)
    // {
    //     var lockValue = Environment.MachineName + Thread.CurrentThread.ManagedThreadId;
    //     return await _redisDatabase.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
    // }
    
    // B. PIPELINE for batch operations:
    // var batch = _redisDatabase.CreateBatch();
    // var tasks = new List<Task>();
    // foreach (var item in items)
    // {
    //     var cacheKey = $"{typeof(T).Name}_{item.Id}";
    //     tasks.Add(batch.StringSetAsync(cacheKey, JsonSerializer.Serialize(item)));
    // }
    // batch.Execute();
    // await Task.WhenAll(tasks);
    
    // C. PUB/SUB for cache invalidation across instances:
    // var subscriber = _redis.GetSubscriber();
    // await subscriber.PublishAsync("cache:invalidate", $"{typeof(T).Name}_{id}");
    
    // D. REDIS STREAMS for event-driven cache updates:
    // await _redisDatabase.StreamAddAsync("cache:events", new NameValueEntry[]
    // {
    //     new("entity", typeof(T).Name),
    //     new("id", id.ToString()),
    //     new("operation", "update"),
    //     new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
    // });
    
    // PERFORMANCE CONSIDERATIONS:
    // - Use connection pooling (StackExchange.Redis handles this)
    // - Implement circuit breaker pattern for cache failures
    // - Monitor cache hit ratios and adjust TTL accordingly
    // - Use compression for large objects (Gzip, Brotli)
    // - Consider Redis Cluster for horizontal scaling
    // - Use Redis persistence (RDB + AOF) for durability
    // - Implement cache warming strategies for cold starts
}

