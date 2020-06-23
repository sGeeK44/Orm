using System;

namespace SmartWay.Orm.Sqlite.Fields
{
    public class TimeField : SqliteField
    {
        public override string GetDataTypeDefinition()
        {
            return "INTEGER";
        }

        public override object Convert(object value)
        {
            if (value == null)
                return null;
            // SQL Compact doesn't support Time, so we're convert to ticks in both directions
            return new TimeSpan((long) value);
        }

        public override object ToSqlValue(object instanceValue, out bool needToUpdateInstance)
        {
            needToUpdateInstance = true;

            if (instanceValue == null)
            {
                if (AllowsNulls)
                    return null;

                throw new NotSupportedException("You must provide default value for non nullable field.");
            }

            return base.ToSqlValue(((TimeSpan) instanceValue).Ticks, out needToUpdateInstance);
        }
    }
}