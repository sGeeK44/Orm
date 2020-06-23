using System;
using System.Data.SqlTypes;

namespace SmartWay.Orm.Sqlite.Fields
{
    public class DateTimeField : SqliteField
    {
        public override string GetDataTypeDefinition()
        {
            //SQLite does not have a storage class set aside for storing dates and/or times.
            //Instead, the built-in Date And Time Functions of SQLite are capable of storing dates and times as TEXT, REAL, or INTEGER values
            //TEXT as ISO8601 strings("YYYY-MM-DD HH:MM:SS.SSS").
            return "TEXT";
        }

        protected override string GetDefaultValueFieldCreation()
        {
            if (Constants.DefaultValue.DateTimeNow.Equals(DefaultValue))
                return "CURRENT_TIMESTAMP";

            return $"'{DefaultValue}'";
        }

        public override object ToSqlValue(object instanceValue, out bool needToUpdateInstance)
        {
            needToUpdateInstance = false;
            if (instanceValue == null || IsDefaultDatetimeValue(instanceValue))
            {
                if (AllowsNulls)
                    return null;

                needToUpdateInstance = true;
                if (Constants.DefaultValue.DateTimeNow.Equals(DefaultValue))
                    return DateTime.Now;

                if (DefaultValue == null)
                    throw new NotSupportedException("You must provide default value for non nullable field.");

                return DateTime.Parse(DefaultValue.ToString());
            }

            if (instanceValue == DBNull.Value)
                return instanceValue;

            needToUpdateInstance = true;
            instanceValue = RoundToSqlDateTime((DateTime) instanceValue);

            return instanceValue;
        }

        private static bool IsDefaultDatetimeValue(object instanceValue)
        {
            return Equals(DateTime.MinValue, instanceValue);
        }

        private static DateTime RoundToSqlDateTime(DateTime iv)
        {
            return new SqlDateTime(iv).Value;
        }
    }
}