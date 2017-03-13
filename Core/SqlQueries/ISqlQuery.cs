using System.Collections.Generic;

namespace Orm.Core.SqlQueries
{
    public interface ISqlQuery<TEntity>
    {
        IEnumerable<TEntity> Execute();
    }

    public interface ISqlQuery
    {
        IEnumerable<object> Execute();
    }
}