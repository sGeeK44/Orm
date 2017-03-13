using System.Collections.Generic;
using System.Data;

namespace Orm.Core.Filters
{
    public class AddDay : IFilter
    {
        private readonly ObjectValue _value;
        private readonly ColumnValue _buildColumnValue;

        public AddDay(ObjectValue value, ColumnValue buildColumnValue)
        {
            _value = value;
            _buildColumnValue = buildColumnValue;
        }

        public string ToStatement(ref List<IDataParameter> @params)
        {
            return string.Format("DATEADD(DAY, {0}, {1})",
                _buildColumnValue.ToStatement(ref @params),
                _value.ToStatement(ref @params));
        }
    }
}