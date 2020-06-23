using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartWay.Orm.Caches
{
    public class EntityCache
    {
        private readonly Dictionary<object, object> _cache = new Dictionary<object, object>();

        public object GetOrDefault(object cacheKey)
        {
            return _cache.ContainsKey(cacheKey)
                ? _cache[cacheKey]
                : null;
        }

        public void Add(object entity, object cacheKey)
        {
            if (_cache.ContainsKey(cacheKey))
                _cache[cacheKey] = entity;
            else
                _cache.Add(cacheKey, entity);
        }

        public void Remove(object itemToRemove)
        {
            if (!_cache.ContainsValue(itemToRemove))
                return;

            var itemCached = _cache.First(_ => itemToRemove.Equals(_.Value));
            _cache.Remove(itemCached.Key);
        }

        public void Remove<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            var itemsToRemove = new List<object>();
            foreach (var cachedValues in _cache)
            {
                if (!predicate(cachedValues.Value as TEntity))
                    continue;

                itemsToRemove.Add(cachedValues.Key);
            }

            foreach (var key in itemsToRemove) _cache.Remove(key);
        }
    }
}