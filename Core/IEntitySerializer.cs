using System;
using System.Data;

namespace Orm.Core
{
    /// <summary>
    /// Provider methods to convert and convert back object with database format
    /// </summary>
    public interface IEntitySerializer
    {
        /// <summary>
        /// Indicate if current serializer should use full field name data column access. (Depending on select used)
        /// </summary>
        bool UseFullName { get; set; }

        /// <summary>
        /// Get or set Entity cache to use for checking existing instance before made convert back
        /// </summary>
        IEntityCache EntityCache { get; set; }

        /// <summary>
        /// Convert specified database result into specified intance type
        /// </summary>
        /// <param name="dbResult">DataReader get from select</param>
        /// <returns>Instance initialized</returns>
        object Deserialize(IDataRecord dbResult);

        /// <summary>
        /// Initialize specified object item with data record value
        /// </summary>
        /// <param name="item">Object to fill</param>
        /// <param name="dbResult">DataReader get from select</param>
        /// <returns>Object corresponding to primary key field</returns>
        object PopulateFields(object item, IDataRecord dbResult);

        /// <summary>
        /// Fill reference property in specified item
        /// </summary>
        /// <typeparam name="TRefToFill">Type of Reference to fill</typeparam>
        /// <param name="item">Item wich contains reference to fill</param>
        /// <param name="dbResult">DataReader get from select</param>
        void FillReference<TRefToFill>(object item, IDataRecord dbResult);

        /// <summary>
        /// Fill reference property in specified item
        /// </summary>
        /// <param name="refToFill">Type of Reference to fill</param>
        /// <param name="item">Item wich contains reference to fill</param>
        /// <param name="dbResult">DataReader get from select</param>
        void FillReference(Type refToFill, object item, IDataRecord dbResult);

        /// <summary>
        /// Serialize specified object value of specified field name into database value format
        /// </summary>
        /// <param name="fieldName">Field name involve</param>
        /// <param name="value">Object value to serialize</param>
        /// <returns>Database value</returns>
        object SerializeObjectField(string fieldName, object value);

        /// <summary>
        /// Serialize specified object value of specified field name into database value format
        /// </summary>
        /// <param name="fieldName">Field name involve</param>
        /// <param name="value">Object value to deserialize</param>
        /// <returns>.Net value</returns>
        object DeserializeObjectField(string fieldName, object value);
    }
}