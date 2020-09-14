using System;
using System.Collections.Generic;
using System.Linq;
using SmartWay.Orm.Entity;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Repositories
{
    public class Repository<TEntity, TIEntity> : IRepository<TIEntity>
        where TEntity : EntityBase<TIEntity>, TIEntity, new()
        where TIEntity : class, IDistinctableEntity
    {
        public Repository()
        {
        }

        public Repository(IDataStore dataStore)
        {
            DataStore = dataStore;
        }

        public IDataStore DataStore { get; set; }

        /// <summary>
        ///     Save specified entity from repository
        /// </summary>
        /// <param name="entity">Entity to save</param>
        public virtual void Save(TIEntity entity)
        {
            if (entity == null)
                return;

            if ((long)entity.GetPkValue() == EntityBase<TIEntity>.NullId
                || GetById(entity.GetPkValue()) == null)
                DataStore.Insert(entity);
            else
                DataStore.Update(entity);
        }

        /// <summary>
        ///     Delete specifie specified entity from repository
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        public virtual void Delete(TIEntity entity)
        {
            if (entity == null)
                return;

            DataStore.Delete(entity);
        }

        /// <summary>
        ///     Delete specified entity list in repository
        /// </summary>
        /// <param name="entities">Entity list to delete</param>
        public virtual void Delete(List<TIEntity> entities)
        {
            if (entities == null || entities.Count == 0)
                return;

            DataStore.DeleteBulk<TEntity, TIEntity>(entities);
        }

        /// <summary>
        ///     Delete entities by bundle
        /// </summary>
        /// <param name="entities">all entities to delete</param>
        /// <param name="bundleSize">bundle size of each entities deleted each delete request</param>
        /// <param name="observer">observer to report progression</param>
        public void DeleteByBundle(List<TIEntity> entities, int bundleSize, IOrmObserver observer)
        {
            if (entities == null || !entities.Any())
                return;

            observer.ReportProgess(0);
            for (var i = 0; i < entities.Count; i += bundleSize)
            {
                var bundleToDelete = entities.Skip(i).Take(bundleSize).ToList();
                Delete(bundleToDelete);
                var progress = Convert.ToInt32((double) (i + bundleToDelete.Count) / entities.Count * 100);
                observer.ReportProgess(progress);
            }
        }

        /// <summary>
        ///     Get a entity object with the pk
        /// </summary>
        /// <param name="pk">The unique identifier of the entity to get</param>
        /// <returns>The entity if exists in datastore, else null</returns>
        public virtual TIEntity GetById(object pk)
        {
            return DataStore.Select<TEntity>(pk);
        }

        public TIEntity GetByGuid(Guid guid)
        {
            var condition =
                DataStore.Condition<TEntity>(EntityBase<TEntity>.GuidColumnName, guid, FilterOperator.Equals);
            return DataStore.Select<TEntity>().Where(condition).GetValues().FirstOrDefault();
        }


        public int Count()
        {
            return DataStore.Select<TEntity>().Count();
        }

        public object GetObjectByGuid(Guid guid)
        {
            return GetByGuid(guid);
        }

        /// <summary>
        ///     Get all TEntity linked to specified foreign key
        /// </summary>
        /// <typeparam name="TForeignEntity">Type of foreign entity</typeparam>
        /// <param name="id">Foreign key value</param>
        /// <returns>A collection with all entity linked</returns>
        public virtual List<TIEntity> GetAllReference<TForeignEntity>(object id)
        {
            return new List<TIEntity>();
        }

        /// <summary>
        ///     Return count of all TEntity linked to specified foreign key
        /// </summary>
        /// <typeparam name="TForeignEntity">Type of foreign entity</typeparam>
        /// <param name="id">Foreign key value</param>
        /// <returns>Count of all entity linked</returns>
        public virtual long CountAllReference<TForeignEntity>(long id)
        {
            return 0;
        }

        /// <summary>
        ///     Search all entity in database
        /// </summary>
        /// <returns>All Entity found or empty list</returns>
        public virtual List<TIEntity> GetAll()
        {
            return DataStore.Select<TEntity, TIEntity>().GetValues().ToList();
        }

        /// <summary>
        ///     Change datasource for current repository
        /// </summary>
        /// <param name="newDataStore">New Data store to take in source</param>
        public void ChangeTarget(IDataStore newDataStore)
        {
            DataStore = newDataStore;
        }

        /// <summary>
        ///     Create a new repository with specified generique arg
        /// </summary>
        /// <param name="entityType">Type of entity</param>
        /// <param name="entityTypeInterface">Type of entity abstraction</param>
        /// <param name="datastore">Repo datastore</param>
        /// <returns>A new instance of repository</returns>
        public static object Create(Type entityType, Type entityTypeInterface, IDataStore datastore)
        {
            var repoType = typeof(Repository<,>).MakeGenericType(entityType, entityTypeInterface);
            var result = (IRepository) Activator.CreateInstance(repoType);
            result.DataStore = datastore;
            return result;
        }
    }
}