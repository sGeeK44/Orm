using System.Collections.Generic;
using SmartWay.Orm.Filters;

namespace SmartWay.Orm.Queries
{
    public interface IQuery<TEntity>
    {
        IEnumerable<TEntity> GetValues();
        IEnumerable<TEntity> Top(int quantity);
        int Count();
        int Delete();
        int Update();
        IEnumerable<AggregableResultRow> Count(params ColumnValue[] columns);
        IEnumerable<AggregableResultRow> Sum(params ColumnValue[] columns);
    }
}