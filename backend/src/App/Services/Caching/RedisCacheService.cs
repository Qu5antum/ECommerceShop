using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace App.Services.Caching;


public interface IRedisCacheService
{
    Task<T?> GetDataAsync<T>(string key);

    Task SetDataAsync<T>(
        string key,
        T data,
        TimeSpan? expiration = null);

    Task RemoveDataAsync(string key);
}



public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetDataAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);

        if (data is null)
            return default;

        return JsonSerializer.Deserialize<T>(data, JsonOptions);
    }

    public async Task SetDataAsync<T>(
        string key,
        T data,
        TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow =
                expiration ?? TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(key, json, options);
    }

    public async Task RemoveDataAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}