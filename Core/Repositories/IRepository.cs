// ReSharper disable TypeParameterCanBeVariant
namespace Orm.Core.Repositories
{
    /// <summary>
    /// Expose command feature expose by a standard repository
    /// </summary>
    /// <typeparam name="TEntity">Type of entity managed by repository</typeparam>
    public interface IRepository<TEntity>
    {
        /// <summary>
        /// Save specified entity in repository
        /// </summary>
        /// <param name="entity">Entity to save</param>
        void Save(TEntity entity);

        /// <summary>
        /// Delete specifie specified entity in repository
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        void Delete(TEntity entity);
        
        /// <summary>
        /// Search TEntity with specified id in repository
        /// </summary>
        /// <param name="id">Id to search</param>
        /// <returns>Entity if found, else null</returns>
        TEntity GetById(long id);
    }
}