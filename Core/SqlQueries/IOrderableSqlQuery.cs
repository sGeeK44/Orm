using Orm.Core.Filters;

namespace Orm.Core.SqlQueries
{
    public interface IOrderableSqlQuery<TEntity> : ISqlQuery<TEntity>
    {
        ISqlQuery<TEntity> OrderBy(params ColumnValue[] fields);
    }

    public interface IOrderableSqlQuery : ISqlQuery
    {
        ISqlQuery OrderBy(params ColumnValue[] fields);
    }
}