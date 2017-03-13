using System.Collections.Generic;
using System.Data;
using System.Text;
using Orm.Core.Filters;

namespace Orm.Core.SqlQueries
{
    public class OrderBy
    {
        private ColumnValue[] _fields;

        /// <summary>
        /// Set fields involve in order by
        /// </summary>
        /// <param name="fields">Ordered field for order by statement</param>
        public void SetFields(ColumnValue[] fields)
        {
            _fields = fields;
        }

        /// <summary>
        /// Calculate sql statement equivalent to current Order By
        /// </summary>
        /// <returns>Sql string statement</returns>
        public string ToStatement()
        {
            if (_fields == null || _fields.Length == 0)
                return string.Empty;

            var @params = new List<IDataParameter>();
            var result = new StringBuilder(" ORDER BY ");
            for (var i = 0; i < _fields.Length; i++)
            {
                if (i > 0) result.Append(", ");
                result.Append(_fields[i].ToStatement(ref @params));
            }

            return result.ToString();
        }
    }
}