using System;
using System.Collections.Generic;
using System.Data;
using Orm.Core.Filters;

// ReSharper disable UseStringInterpolation

namespace Orm.Core.SqlQueries
{
    public class Where : ISqlClause
    {
        private readonly IFilter _filter;

        public Where() { }
        
        public Where(IFilter filter)
        {
            _filter = filter;
        }

        public string ToStatement()
        {
            throw new NotSupportedException();
        }

        public string ToStatement(out List<IDataParameter> @params)
        {
            @params = new List<IDataParameter>();
            return _filter != null
                 ? " WHERE " + _filter.ToStatement(ref @params)
                 : string.Empty;
        }
    }
}