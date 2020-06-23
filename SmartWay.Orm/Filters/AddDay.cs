using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Filters
{
    public class AddDay : IFilter
    {
        private readonly ColumnValue _buildColumnValue;
        private readonly ObjectValue _value;

        public AddDay(ObjectValue value, ColumnValue buildColumnValue)
        {
            _value = value;
            _buildColumnValue = buildColumnValue;
        }

        public string ToStatement(List<IDataParameter> @params)
        {
            return $"DATEADD(DAY, {_buildColumnValue.ToStatement(@params)}, {_value.ToStatement(@params)})";
        }
    }
}