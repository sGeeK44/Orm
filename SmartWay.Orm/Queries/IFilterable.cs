using System;
using System.Linq.Expressions;
using SmartWay.Orm.Filters;

namespace SmartWay.Orm.Queries
{
    public interface IFilterable<TEntity> : IAggragableQuery<TEntity>
    {
        IAggragableQuery<TEntity> Where(IFilter filter);
        IAggragableQuery<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
    }
}