using SmartWay.Orm.Filters;

namespace SmartWay.Orm.Queries
{
    public interface IFilterable<TEntity> : IAggragableQuery<TEntity>
    {
        IAggragableQuery<TEntity> Where(IFilter filter);
    }
}