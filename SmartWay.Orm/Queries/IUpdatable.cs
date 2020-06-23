namespace SmartWay.Orm.Queries
{
    public interface IUpdatable<TEntity> : IFilterable<TEntity>
    {
        IUpdatable<TEntity> Set(string columnName, object value);
    }
}