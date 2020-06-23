using System;

namespace SmartWay.Orm.Sqlite.Fields
{
    public class RowVersionField : SqliteField
    {
        /// <summary>
        ///     Indicate if property can be setted or managed by sgbd
        /// </summary>
        public override bool IsSettable => false;

        public override object Convert(object value)
        {
            // sql stores this an 8-byte array
            return BitConverter.ToInt64((byte[]) value, 0);
        }

        public override string GetDataTypeDefinition()
        {
            return "BLOB";
        }

        public override object ToSqlValue(object instanceValue, out bool needToUpdateInstance)
        {
            // read-only, so do nothing*
            needToUpdateInstance = false;
            return null;
        }
    }
}