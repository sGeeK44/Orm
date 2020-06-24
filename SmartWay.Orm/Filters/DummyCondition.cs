using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Filters
{
    public class DummyCondition : IFilter
    {
        public string ToStatement(List<IDataParameter> @params)
        {
            return string.Empty;
        }
    }
}