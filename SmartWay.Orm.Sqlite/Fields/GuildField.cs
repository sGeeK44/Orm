using System;

namespace SmartWay.Orm.Sqlite.Fields
{
    public class GuildField : SqliteField
    {
        public override string GetDataTypeDefinition()
        {
            return "TEXT";
        }

        protected override object ConvertValue(Type propType, object value)
        {
            return new Guid((string) value);
        }

        public override object ToSqlValue(object instanceValue, out bool needToUpdateInstance)
        {
            needToUpdateInstance = false;
            if (instanceValue == null || IsDefaultGuidValue(instanceValue))
            {
                if (AllowsNulls)
                    return instanceValue;

                needToUpdateInstance = true;
                if (Constants.DefaultValue.RandomGuid.Equals(DefaultValue))
                    return ToSqlValue(Guid.NewGuid());

                if (DefaultValue == null)
                    throw new NotSupportedException("You must provide default value for non nullable field.");

                return ToSqlValue(DefaultValue);
            }

            return ToSqlValue(instanceValue);
        }

        public object ToSqlValue(object instanceValue)
        {
            return instanceValue.ToString().ToLower();
        }

        private static bool IsDefaultGuidValue(object instanceValue)
        {
            return instanceValue.Equals(Guid.Empty);
        }
    }
}