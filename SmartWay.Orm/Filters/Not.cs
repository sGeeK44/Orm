using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Filters
{
    public class Not : IFilter
    {
        private readonly IFilter _filter;

        public Not(IFilter filter)
        {
            _filter = filter;
        }

        public string ToStatement(List<IDataParameter> @params)
        {
            return $"NOT({_filter.ToStatement(@params)})";
        }
    }
}