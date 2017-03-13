using Orm.Core.Filters;

namespace Orm.Core.SqlQueries
{
    public interface IFilterable<TEntity> : IOrderableSqlQuery<TEntity>
    {
        IOrderableSqlQuery<TEntity> Where(IFilter filter);
    }

    public interface IFilterable : IOrderableSqlQuery
    {
        IOrderableSqlQuery Where(IFilter filter);
    }
}