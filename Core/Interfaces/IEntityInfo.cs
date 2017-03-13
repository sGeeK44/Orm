using System;
using System.Data;
using Orm.Core.Attributes;

namespace Orm.Core.Interfaces
{
    public delegate object EntityCreatorDelegate(FieldAttributeCollection fields, IDataReader results);

    public interface IEntityInfo
    {
        /// <summary>
        ///  Get typeof entity
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Get fields attributes
        /// </summary>
        FieldAttributeCollection Fields { get; }

        /// <summary>
        /// Get references attributes
        /// </summary>
        ReferenceAttributeCollection References { get; }

        /// <summary>
        /// Get entity attibute
        /// </summary>
        EntityAttribute EntityAttribute { get; }

        /// <summary>
        /// Get entity name
        /// </summary>
        string EntityName { get; }

        /// <summary>
        /// Calculate full qualified field name in according with current entity name
        /// </summary>
        /// <param name="fieldName">Field name to convert to fully qualify name</param>
        /// <returns></returns>
        string FullyQualifyFieldName(string fieldName);

        /// <summary>
        /// Return serializer to use to convert back entity from db
        /// </summary>
        /// <returns>Entity serializer</returns>
        IEntitySerializer GetSerializer();

        /// <summary>
        /// Get reference attribute associated to specified object type
        /// </summary>
        /// <param name="refType">Type of reference</param>
        /// <returns>Reference attribute found, else null</returns>
        ReferenceAttribute GetReference(Type refType);
    }
}
