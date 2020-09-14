using System;

namespace SmartWay.Orm.Entity
{
    /// <summary>
    ///     Expose one property used to distinct same entity type
    /// </summary>
    public interface IDistinctableEntity
    {
        /// <summary>
        /// Get primary key column name
        /// </summary>
        /// <returns>Entity primary key column name</returns>
        string GetPkColumnName();

        /// <summary>
        /// Get primary key value
        /// </summary>
        /// <returns>Entity primary key value</returns>
        object GetPkValue();
    }
}