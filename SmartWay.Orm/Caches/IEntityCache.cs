using System;

namespace SmartWay.Orm.Caches
{
    /// <summary>
    ///     Expose methods needed to retreive cached object
    /// </summary>
    public interface IEntityCache
    {
        /// <summary>
        ///     Get cached entityType of for specified cacheKey
        /// </summary>
        /// <param name="entityType">Type of entity</param>
        /// <param name="cacheKey">A unique key that represent entity</param>
        /// <returns>entity cached if exist, else null</returns>
        object GetOrDefault(Type entityType, object cacheKey);

        /// <summary>
        ///     Update specified item from RepositoryCache
        /// </summary>
        /// <param name="entity">Entity to Add in cache</param>
        /// <param name="cacheKey">A unique key that represent specified entity</param>
        void Cache(object entity, object cacheKey);

        /// <summary>
        ///     Delete specified item from Cache
        /// </summary>
        /// <param name="itemToRemove">Item to remove from cache</param>
        void Invalidate(object itemToRemove);

        /// <summary>
        ///     Delete specified items from Cache
        /// </summary>
        /// <param name="predicate">Items that predicate match should be removed</param>
        void Invalidate<TEntity>(Func<TEntity, bool> predicate) where TEntity : class;

        /// <summary>
        ///     Clear cache
        /// </summary>
        void Clear();
    }
}