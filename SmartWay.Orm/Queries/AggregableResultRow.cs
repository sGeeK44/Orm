using System;
using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Queries
{
    public class AggregableResultRow
    {
        public AggregableResultRow(IDataReader row)
        {
            ColumnValues = new Dictionary<string, object>();
            for (var i = 0; i < row.FieldCount; i++)
            {
                var dbColumnValue = row.GetValue(i);
                ColumnValues.Add(row.GetName(i), dbColumnValue == DBNull.Value ? null : dbColumnValue);
            }
        }

        public Dictionary<string, object> ColumnValues { get; set; }
    }
}