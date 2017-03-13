namespace Orm.Core.SqlQueries
{
    public interface IJoinable<TEntity> : IFilterable<TEntity>
    {
        IJoinable<TEntity> Join<TEntity1, TEntity2>();
        IJoinable<TEntity> LeftJoin<TEntity1, TEntity2>();
    }

    public interface IJoinable : IFilterable
    {
        IJoinable Join<TEntity1, TEntity2>();
        IJoinable LeftJoin<TEntity1, TEntity2>();
    }
}