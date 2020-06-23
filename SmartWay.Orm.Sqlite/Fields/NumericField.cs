using System;

namespace SmartWay.Orm.Sqlite.Fields
{
    public class NumericField : SqliteField
    {
        private const int DefaultNumericFieldPrecision = 16;

        public NumericField(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }

        /// <summary>
        ///     Precision for floating number
        /// </summary>
        public int Precision { get; set; }

        /// <summary>
        ///     Scale for floating number
        /// </summary>
        public int Scale { get; set; }

        public override string GetDataTypeDefinition()
        {
            return "NUMERIC";
        }

        public override object ToSqlValue(object instanceValue, out bool needToUpdateInstance)
        {
            instanceValue = base.ToSqlValue(instanceValue, out needToUpdateInstance);
            return instanceValue == DBNull.Value ? instanceValue : Math.Round((decimal) instanceValue, Scale);
        }
    }
}