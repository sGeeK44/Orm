using SmartWay.Orm.Filters;

namespace SmartWay.Orm.Queries
{
    public interface IAggragableQuery<TEntity> : IOrderableQuery<TEntity>
    {
        IOrderableQuery<TEntity> GroupBy(params ColumnValue[] columns);
    }
}