using SmartWay.Orm.Filters;

namespace SmartWay.Orm.Queries
{
    public interface IJoinable<TEntity> : IFilterable<TEntity>
    {
        IJoinable<TEntity> Join<TEntity1, TEntity2>();
        IJoinable<TEntity> Join(ICondition condition);
        IJoinable<TEntity> LeftJoin<TEntity1, TEntity2>();
        IJoinable<TEntity> LeftJoin(ICondition condition);
    }
}