using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Entity.References;
using SmartWay.Orm.Entity.Serializers;
using SmartWay.Orm.Interfaces;
using Index = SmartWay.Orm.Entity.Constraints.Index;

namespace SmartWay.Orm
{
    public class EntityInfo : IEntityInfo
    {
        private readonly object[] _attributes;

        private string _nameInStore;
        private IEntitySerializer _serializer;

        private EntityInfo(IFieldPropertyFactory fieldPropertyFactory, EntityInfoCollection entities, Type entityType)
        {
            Fields = new DistinctCollection<Field>();
            ForeignKeys = new DistinctCollection<ForeignKey>();
            References = new DistinctCollection<Reference>();
            Indexes = new DistinctCollection<Index>();
            Entities = entities;
            EntityType = entityType;
            _attributes = entityType.GetCustomAttributes(true);
            FieldPropertyFactory = fieldPropertyFactory;
        }

        private EntityAttribute EntityAttribute => GetAttribute<EntityAttribute>();
        public IFieldPropertyFactory FieldPropertyFactory { get; set; }

        /// <summary>
        ///     Get all entites info
        /// </summary>
        public EntityInfoCollection Entities { get; }

        /// <summary>
        ///     Get typeof entity
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        ///     Get entity primary key
        /// </summary>
        public PrimaryKey PrimaryKey { get; private set; }

        /// <summary>
        ///     Get entity foreign keys
        /// </summary>
        public DistinctCollection<ForeignKey> ForeignKeys { get; set; }

        /// <summary>
        ///     Get entity fields (Including PK and FK)
        /// </summary>
        public DistinctCollection<Field> Fields { get; }

        /// <summary>
        ///     Get references attributes
        /// </summary>
        public DistinctCollection<Reference> References { get; }

        /// <summary>
        ///     Get Indexes existing on current entity
        /// </summary>
        public DistinctCollection<Index> Indexes { get; }

        /// <summary>
        ///     Get entity name in store
        /// </summary>
        public string GetNameInStore()
        {
            if (_nameInStore != null)
                return _nameInStore;

            _nameInStore = EntityAttribute.GetNameInStore(EntityType);
            return _nameInStore;
        }

        public IEntitySerializer GetSerializer()
        {
            if (_serializer != null)
                return _serializer;

            if (EntityAttribute.Serializer == null)
                return new DefaultEntitySerializer(this);

            var constructor = EntityAttribute.Serializer.GetConstructors().First(GetConstructWithIEntityInfoParam);
            _serializer = constructor.Invoke(new object[] {this}) as IEntitySerializer;

            return _serializer ?? new DefaultEntitySerializer(this);
        }

        /// <summary>
        ///     Get reference attribute associated to specified object type
        /// </summary>
        /// <param name="refType">Type of reference</param>
        /// <returns>Reference attribute found, else null</returns>
        public Reference GetReference(Type refType)
        {
            return References.FirstOrDefault(_ => _.ReferenceEntityType == refType);
        }

        /// <summary>
        ///     Looking for specified attribute on entity class
        /// </summary>
        /// <typeparam name="T">Type of attribute searched.</typeparam>
        /// <returns>Attribute found or null.</returns>
        public T GetAttribute<T>()
        {
            return (T) _attributes.FirstOrDefault(_ => _ is T);
        }

        /// <summary>
        ///     Create a new instance of entity
        /// </summary>
        /// <returns>New entity</returns>
        public object CreateNewInstance()
        {
            return Activator.CreateInstance(EntityType);
        }

        public Field GetField(MemberInfo property)
        {
            return Fields.FirstOrDefault(_ => _.IsMatch(property));
        }

        private static bool GetConstructWithIEntityInfoParam(ConstructorInfo constructor)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != 1)
                return false;

            return parameters[0].ParameterType == typeof(IEntityInfo);
        }

        public override string ToString()
        {
            return GetNameInStore();
        }

        public static IEntityInfo Create(IFieldPropertyFactory fieldPropertyFactory, EntityInfoCollection entities,
            Type entityType)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");

            // already added
            var entityName = entities.GetNameForType(entityType);
            if (entityName != null)
                return entities[entityName];

            //TODO: validate NameInStore
            var result = new EntityInfo(fieldPropertyFactory, entities, entityType);

            if (result.EntityAttribute == null)
                throw new ArgumentException($"Type '{entityType.Name}' does not have an EntityAttribute");

            entities.Add(result);

            // see if we have any entity 
            // get all field definitions
            foreach (var prop in GetProperties(entityType)) result.AnalyseAttribute(prop);

            if (result.Fields.Count == 0)
                throw new EntityDefinitionException(
                    $"Entity '{result.GetNameInStore()}' Contains no Field definitions.");

            //Update Reference atribute with new known type
            foreach (var entityInfo in entities)
            {
                var entity = entityInfo as EntityInfo;

                entity?.UpdateRefenceAttribute();
            }

            return result;
        }

        private static IEnumerable<PropertyInfo> GetProperties(Type entityType)
        {
            var result = entityType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                                  BindingFlags.DeclaredOnly);
            return entityType.BaseType != typeof(object)
                ? BuildUnionWithBaseType(entityType, result)
                : result;
        }

        private static IEnumerable<PropertyInfo> BuildUnionWithBaseType(Type entityType,
            IEnumerable<PropertyInfo> subTypeProp)
        {
            var equalityComparer = new DisctinctPropByName();
            return GetProperties(entityType.BaseType)
                .Where(baseProp => !subTypeProp.Contains(baseProp, equalityComparer))
                .Concat(subTypeProp);
        }

        private void UpdateRefenceAttribute()
        {
            foreach (var reference in References)
            {
                if (reference.EntityReference != null)
                    continue;

                reference.EntityReference = Entities[reference.ReferenceEntityType];
            }
        }

        private void AnalyseAttribute(PropertyInfo prop)
        {
            var sqlAttribute = GetEntityFieldAttribute(prop);
            TreatFieldAttribute(prop, sqlAttribute as FieldAttribute);
            TreatReference(prop, sqlAttribute as ReferenceAttribute);
        }

        private static EntityFieldAttribute GetEntityFieldAttribute(PropertyInfo prop)
        {
            var sqlAttribute = prop.GetCustomAttributes(true)
                .FirstOrDefault(a => a is EntityFieldAttribute) as EntityFieldAttribute;
            return sqlAttribute;
        }

        private void TreatReference(PropertyInfo prop, ReferenceAttribute referenceAttribute)
        {
            if (referenceAttribute == null)
                return;

            var reference = Reference.Create(this, prop, referenceAttribute);
            References.Add(reference);
        }

        private void TreatFieldAttribute(PropertyInfo prop, FieldAttribute fieldAttribute)
        {
            if (fieldAttribute == null)
                return;

            if (Fields.Contains(fieldAttribute.Key))
                return;

            var field = CreateField(prop, fieldAttribute);

            if (fieldAttribute.SearchOrder != FieldSearchOrder.NotSearchable
                || fieldAttribute.RequireUniqueValue)
            {
                var newIndex = Index.CreateStandard(GetNameInStore(), field);
                Indexes.Add(newIndex);
            }

            foreach (var index in fieldAttribute.Indexes)
                if (!Indexes.Contains(index))
                {
                    var newIndex = Index.CreateCustom(index, GetNameInStore(), field);
                    Indexes.Add(newIndex);
                }
                else
                {
                    Indexes[index].AddField(field);
                }

            foreach (var indexProperties in _attributes.Where(_ => _ is IndexPropertiesAttribute)
                .Cast<IndexPropertiesAttribute>())
            {
                if (!Indexes.Contains(indexProperties.IndexName))
                    continue;

                var index = Indexes[indexProperties.IndexName];
                index.IsUnique = indexProperties.RequireUniqueValue;
                index.SearchOrder = indexProperties.SearchOrder;
            }
        }

        private Field CreateField(PropertyInfo prop, FieldAttribute fieldAttribute)
        {
            Field field;
            if (fieldAttribute.IsPrimaryKey)
            {
                PrimaryKey = PrimaryKey.Create(this, prop, fieldAttribute as PrimaryKeyAttribute);
                field = PrimaryKey;
            }
            else if (fieldAttribute.IsForeignKey)
            {
                var foreignKey = ForeignKey.Create(Entities, this, prop, fieldAttribute as ForeignKeyAttribute);
                ForeignKeys.Add(foreignKey);
                field = foreignKey;
            }
            else
            {
                field = Field.Create(this, prop, fieldAttribute);
            }

            Fields.Add(field);
            return field;
        }

        private class DisctinctPropByName : IEqualityComparer<PropertyInfo>
        {
            public bool Equals(PropertyInfo x, PropertyInfo y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;

                return string.Equals(x.Name, y.Name);
            }

            public int GetHashCode(PropertyInfo obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}