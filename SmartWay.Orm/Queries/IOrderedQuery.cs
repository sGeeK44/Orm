using SmartWay.Orm.Filters;

namespace SmartWay.Orm.Queries
{
    public interface IOrderedQuery<TEntity> : IQuery<TEntity>
    {
        IOrderedQuery<TEntity> ThenBy(ColumnValue field);
        IOrderedQuery<TEntity> ThenByDesc(ColumnValue field);
    }
}