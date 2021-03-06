using System.Collections.Generic;
using SmartWay.Orm.Interfaces;

// ReSharper disable TypeParameterCanBeVariant

namespace SmartWay.Orm.Repositories
{
    /// <summary>
    ///     Expose command feature expose by a standard repository
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        ///     Get associated datastore
        /// </summary>
        IDataStore DataStore { get; set; }

        /// <summary>
        ///     Indicate number of entities existing in datastore
        /// </summary>
        /// <returns>Entities's Count</returns>
        int Count();

        /// <summary>
        ///     Return count of all TEntity linked to specified foreign key
        /// </summary>
        /// <typeparam name="TForeignEntity">Type of foreign entity</typeparam>
        /// <param name="fk">Foreign key value</param>
        /// <returns>Count of all entity linked</returns>
        long CountAllReference<TForeignEntity>(object fk);
    }

    /// <summary>
    ///     Expose command feature expose by a standard repository
    /// </summary>
    /// <typeparam name="TEntity">Type of entity managed by repository</typeparam>
    public interface IRepository<TEntity> : IRepository
    {
        /// <summary>
        ///     Save specified entity in repository
        /// </summary>
        /// <param name="entity">Entity to save</param>
        void Save(TEntity entity);

        /// <summary>
        ///     Delete specified entity in repository
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        void Delete(TEntity entity);

        /// <summary>
        ///     Delete specified entity list in repository
        /// </summary>
        /// <param name="entities">Entity list to delete</param>
        void Delete(List<TEntity> entities);

        /// <summary>
        ///     Delete entities by bundle
        /// </summary>
        /// <param name="entities">all entities to delete</param>
        /// <param name="bundleSize">bundle size of each entities deleted each delete request</param>
        /// <param name="observer">oserver to indicate progress</param>
        void DeleteByBundle(List<TEntity> entities, int bundleSize, IOrmObserver observer);

        /// <summary>
        ///     Search TEntity with specified id in repository
        /// </summary>
        /// <param name="pk">Id to search</param>
        /// <returns>Entity if found, else null</returns>
        TEntity GetByPk(object pk);

        /// <summary>
        ///     Get all TEntity linked to specified foreign key
        /// </summary>
        /// <typeparam name="TForeignEntity">Type of foreign entity</typeparam>
        /// <param name="fk">Foreign key value</param>
        /// <returns>A collection with all entity linked</returns>
        List<TEntity> GetAllReference<TForeignEntity>(object fk);

        /// <summary>
        ///     Search all entity in database
        /// </summary>
        /// <returns>All Entity found or empty list</returns>
        List<TEntity> GetAll();
    }
}