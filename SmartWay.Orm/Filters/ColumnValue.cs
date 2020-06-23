using System.Collections.Generic;
using System.Data;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Encapsulate behaviour to manage column value part filter
    /// </summary>
    public class ColumnValue : IFilter
    {
        private readonly string _columnName;
        private readonly IEntityInfo _entity;

        public ColumnValue(IEntityInfo entity, string columnName)
        {
            _entity = entity;
            _columnName = columnName;
        }

        public Field ColumnField => _entity.Fields[_columnName];

        public string AliasFiledName => ColumnField.AliasFieldName;

        /// <summary>
        ///     Convert part to sql string equivalent
        /// </summary>
        /// <param name="params">existing param list to populate in case of part object value</param>
        /// <returns>Sql string representation</returns>
        public string ToStatement(List<IDataParameter> @params)
        {
            return ColumnField.FullFieldName;
        }

        /// <summary>
        ///     Convert part to sql string equivalent
        /// </summary>
        /// <returns>Sql string representation</returns>
        public string ToSelectStatement()
        {
            return ColumnField.ToSelectStatement();
        }

        /// <summary>
        ///     Convert column value to Count aggragated column (ex: [Table].[Col] ==> COUNT([Table].[Col])
        /// </summary>
        /// <returns>Converted column Value</returns>
        public IFilter ToCountedColumn()
        {
            return new CountValue(this);
        }

        /// <summary>
        ///     Convert column value to Count aggragated column (ex: [Table].[Col] ==> SUM([Table].[Col])
        /// </summary>
        /// <returns>Converted column Value</returns>
        public IFilter ToSumColumn()
        {
            return new SumValue(this);
        }
    }
}