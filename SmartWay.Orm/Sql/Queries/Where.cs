using System;
using System.Collections.Generic;
using System.Data;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class Where : IClause
    {
        private readonly IFilter _filter;

        public Where()
        {
        }

        public Where(IFilter filter)
        {
            _filter = filter;
        }

        public string ToStatement(List<IDataParameter> @params)
        {
            return _filter != null
                ? " WHERE " + _filter.ToStatement(@params)
                : string.Empty;
        }

        public string ToStatement()
        {
            throw new NotSupportedException();
        }
    }
}