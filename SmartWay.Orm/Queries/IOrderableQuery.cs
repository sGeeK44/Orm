using SmartWay.Orm.Filters;

namespace SmartWay.Orm.Queries
{
    public interface IOrderableQuery<TEntity> : IOrderedQuery<TEntity>
    {
        IOrderedQuery<TEntity> OrderBy(ColumnValue field);
        IOrderedQuery<TEntity> OrderByDesc(ColumnValue field);
    }
}