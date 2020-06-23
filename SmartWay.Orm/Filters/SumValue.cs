using System.Collections.Generic;
using System.Data;

namespace SmartWay.Orm.Filters
{
    public class SumValue : IFilter
    {
        private readonly ColumnValue _columnToSum;

        public SumValue(ColumnValue columnToSum)
        {
            _columnToSum = columnToSum;
        }

        /// <summary>
        ///     Convert part to sql string equivalent
        /// </summary>
        /// <param name="params">existing param list to populate in case of part object value</param>
        /// <returns>Sql string representation</returns>
        public string ToStatement(List<IDataParameter> @params)
        {
            return $"SUM({_columnToSum.ToStatement(@params)}) as {_columnToSum.AliasFiledName}";
        }
    }
}