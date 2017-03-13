using System;
using System.Data;

namespace Orm.Core
{
    /// <summary>
    /// Expose methods to converter DbType To .Net and vice versa
    /// </summary>
    public interface IDbTypeConverter
    {
        /// <summary>
        /// Convert specified .Net type to sql db type
        /// </summary>
        /// <param name="type">.Net type to converter</param>
        /// <returns>DbType equivalent</returns>
        DbType ToDbType(Type type);
    }
}