using System.Collections.Generic;
using System.Data;
using Orm.Core.Interfaces;

namespace Orm.Core.Filters
{
    /// <summary>
    /// Encapsulate behaviour to manage column value part filter
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

        /// <summary>
        /// Convert part to sql string equivalent
        /// </summary>
        /// <param name="params">existing param list to populate in case of part object value</param>
        /// <returns>Sql string representation</returns>
        public string ToStatement(ref List<IDataParameter> @params)
        {
            return string.Concat("[", _entity.EntityName, "]", ".", _columnName);
        }
    }
}
