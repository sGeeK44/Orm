using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Filters
{
    public class CountValue : IFilter
    {
        public const string ColumnName = "count";

        private readonly ColumnValue _columnToCount;

        public CountValue(ColumnValue columnToCount)
        {
            _columnToCount = columnToCount;
        }

        /// <summary>
        ///     Convert part to sql string equivalent
        /// </summary>
        /// <param name="params">existing param list to populate in case of part object value</param>
        /// <returns>Sql string representation</returns>
        public string ToStatement(List<IDataParameter> @params)
        {
            return $"COUNT({_columnToCount.ToStatement(@params)}) AS {ColumnName}";
        }
    }
}