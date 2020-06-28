using System;
using System.Reflection;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Entity.References;
using SmartWay.Orm.Entity.Serializers;
using Index = SmartWay.Orm.Entity.Constraints.Index;

namespace SmartWay.Orm.Interfaces
{
    public interface IEntityInfo
    {
        /// <summary>
        ///     Get all entites info
        /// </summary>
        EntityInfoCollection Entities { get; }

        /// <summary>
        ///     Get typeof entity
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        ///     Get entity primary key
        /// </summary>
        PrimaryKey PrimaryKey { get; }

        /// <summary>
        ///     Get entity foreign keys
        /// </summary>
        DistinctCollection<ForeignKey> ForeignKeys { get; }

        /// <summary>
        ///     Get entity fields (Including PK and FK)
        /// </summary>
        DistinctCollection<Field> Fields { get; }

        /// <summary>
        ///     Get references attributes
        /// </summary>
        DistinctCollection<Reference> References { get; }

        /// <summary>
        ///     Get Indexes existing on current entity
        /// </summary>
        DistinctCollection<Index> Indexes { get; }

        /// <summary>
        ///     Return factory use to create specific fields db engine
        /// </summary>
        IFieldPropertyFactory FieldPropertyFactory { get; }

        /// <summary>
        ///     Get entity name in store
        /// </summary>
        string GetNameInStore();

        /// <summary>
        ///     Return serializer to use to convert back entity from db
        /// </summary>
        /// <returns>Entity serializer</returns>
        IEntitySerializer GetSerializer();

        /// <summary>
        ///     Get reference attribute associated to specified object type
        /// </summary>
        /// <param name="refType">Type of reference</param>
        /// <returns>Reference attribute found, else null</returns>
        Reference GetReference(Type refType);

        /// <summary>
        ///     Looking for specified attribute on entity class
        /// </summary>
        /// <typeparam name="T">Type of attribute searched.</typeparam>
        /// <returns>Attribute found or null.</returns>
        T GetAttribute<T>();

        /// <summary>
        ///     Create a new instance of entity
        /// </summary>
        /// <returns>New entity</returns>
        object CreateNewInstance();

        /// <summary>
        /// Get field for specified property
        /// </summary>
        /// <param name="property">Property mark as Field</param>
        /// <returns>Field found or null</returns>
        Field GetField(MemberInfo property);
    }
}