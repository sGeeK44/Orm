using System;
using SmartWay.Orm.Constants;

namespace SmartWay.Orm.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class IndexPropertiesAttribute : Attribute
    {
        /// <summary>
        ///     Create new class to specify index propertis
        /// </summary>
        /// <param name="indexName">Index name where properties should be applied</param>
        public IndexPropertiesAttribute(string indexName)
        {
            IndexName = indexName;
            SearchOrder = FieldSearchOrder.Ascending;
        }

        /// <summary>
        ///     Index name where properties should be applied
        /// </summary>
        public string IndexName { get; }

        /// <summary>
        ///     Add unique constraint on index in database if value equal true.
        ///     Default value is false.
        /// </summary>
        public bool RequireUniqueValue { get; set; }

        /// <summary>
        ///     Add index search order on index.
        ///     Default value is FieldSearchOrder.Ascending
        /// </summary>
        public FieldSearchOrder SearchOrder { get; set; }
    }
}