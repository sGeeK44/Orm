using System;

namespace Orm.Core
{
    /// <summary>
    /// Expose methods needed to retreive cached object
    /// </summary>
    public interface IEntityCache
    {
        /// <summary>
        /// Get an entities previously cached
        /// </summary>
        /// <param name="entityType">TypeOf entity to get</param>
        /// <param name="cacheKey">A unique cache key (generaly entity primary key value)</param>
        /// <returns>Entity if found, else null</returns>
        object Get(Type entityType, object cacheKey);

        /// <summary>
        /// Add specified entity in cache
        /// </summary>
        /// <param name="entity">Entity to add in cache</param>
        /// <param name="cacheKey">A unique cache key (generaly entity primary key value)</param>
        void Add(object entity, object cacheKey);
    }
}