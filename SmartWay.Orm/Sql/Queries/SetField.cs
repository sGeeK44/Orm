using System.Collections.Generic;
using System.Data;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql.Queries
{
    public class SetField<TEntity> : IClause
    {
        private readonly ColumnValue _column;
        private readonly ObjectValue _value;

        public SetField(IDataStore dataStore, string columnName, object value)
        {
            _column = dataStore.GetColumn<TEntity>(columnName);
            _value = dataStore.ToObjectValue(value);
        }

        public string ToStatement(List<IDataParameter> @params)
        {
            return $"{_column.ToStatement(@params)} = {_value.ToStatement(@params)}";
        }
    }
}