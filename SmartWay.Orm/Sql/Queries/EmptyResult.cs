using System;
using System.Collections.Generic;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class EmptyResult<TIEntity> : IQuery<TIEntity>
    {
        public IEnumerable<TIEntity> GetValues()
        {
            return new List<TIEntity>();
        }

        public IEnumerable<TIEntity> Top(int quantity)
        {
            return new List<TIEntity>();
        }

        public int Count()
        {
            return 0;
        }

        public int Delete()
        {
            return 0;
        }

        public int Update()
        {
            return 0;
        }

        public IEnumerable<AggregableResultRow> Count(params ColumnValue[] columns)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<AggregableResultRow> Sum(params ColumnValue[] columns)
        {
            throw new NotImplementedException();
        }
    }
}