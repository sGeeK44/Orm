using Orm.Core.Entity;
using Orm.Core.Interfaces;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace Orm.Core.Repositories
{
    public abstract class Repository<TEntity, TIEntity> : IRepository<TIEntity>
        where TEntity : TIEntity, new()
        where TIEntity : class, IDistinctableEntity
    {
        protected IDataStore DataStore { get; private set; }

        protected Repository(IDataStore dataStore)
        {
            DataStore = dataStore;
        }

        /// <summary>
        /// Save specified entity from repository
        /// </summary>
        /// <param name="entity">Entity to save</param>
        public virtual void Save(TIEntity entity)
        {
            if (entity == null)
                return;

            if (GetById(entity.Id) == null)
                DataStore.Insert(entity);
            else
                DataStore.Update(entity);
        }

        /// <summary>
        /// Delete specifie specified entity from repository
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        public virtual void Delete(TIEntity entity)
        {
            if (entity == null)
                return;

            DataStore.Delete(entity);
        }

        /// <summary>
        /// Get a entity object with the id
        /// </summary>
        /// <param name="id">The id of the entity to get</param>
        /// <returns>The entity if exists in datastore, else null</returns>
        public virtual TIEntity GetById(long id)
        {
            if (id <= 0)
                return null;

            var result = DataStore.Select<TEntity>(id);
            return result == null ? (TIEntity) null : result;
        }
    }
}