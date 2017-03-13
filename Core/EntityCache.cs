using System;
using System.Collections.Generic;

namespace Orm.Core
{
    public class EntityCache : IEntityCache
    {
        private readonly Dictionary<Type, Dictionary<object, object>> _cache = new Dictionary<Type, Dictionary<object, object>>();

        public object Get(Type entityType, object cacheKey)
        {
            if (!_cache.ContainsKey(entityType))
                return null;

            var entityTypeCache = _cache[entityType];
            if (!entityTypeCache.ContainsKey(cacheKey))
                return null;

            return entityTypeCache[cacheKey];
        }

        public void Add(object entity, object cacheKey)
        {
            if (entity == null)
                return;

            var entityType = entity.GetType();
            if (!_cache.ContainsKey(entityType))
                _cache.Add(entityType, new Dictionary<object, object>());

            var entityTypeCache = _cache[entityType];
            if (!entityTypeCache.ContainsKey(cacheKey))
                entityTypeCache.Add(cacheKey, entity);
        }
    }
}