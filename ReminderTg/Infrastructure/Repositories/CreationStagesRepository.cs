using Microsoft.Extensions.Caching.Memory;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

public sealed class CreationStagesRepository : ICreationStagesRepository
{
    public CreationStagesRepository(IMemoryCache cache)
    {
        _cache = cache;
    }

    private IMemoryCache _cache;

    public void AddStage(CreationStage state)
    {
        object id = state.UserId;

        if (_cache.TryGetValue(id, out _))
            _cache.Remove(id);

        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(3));
        _cache.Set(id, state, cacheEntryOptions);
    }

    public CreationStage GetStage(long stateId)
    {
        CreationStage cacheEntry;
        object id = stateId;
        return _cache.TryGetValue(id, out cacheEntry) ? cacheEntry : cacheEntry;
    }

    public void RemoveStage(CreationStage state)
    {
        object id = state.UserId;

        if (_cache.TryGetValue(id, out _))
            _cache.Remove(id);
    }
}