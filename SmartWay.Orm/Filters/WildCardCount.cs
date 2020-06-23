using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Filters
{
    public class WildCardCount : IFilter
    {
        public string ToStatement(List<IDataParameter> @params)
        {
            return "COUNT(*) AS count";
        }
    }
}