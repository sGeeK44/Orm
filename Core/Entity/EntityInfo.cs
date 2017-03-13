using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Orm.Core.Attributes;
using Orm.Core.Interfaces;
using Orm.Core.SqlStore;

// ReSharper disable UseNameofExpression
// ReSharper disable UseNullPropagation

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UseStringInterpolation

namespace Orm.Core
{
    public class EntityInfo : IEntityInfo
    {
        private IEntitySerializer _serializer;

        public EntityInfo()
        {
            Fields = new FieldAttributeCollection();
            References = new ReferenceAttributeCollection();
        }

        public EntityInfoCollection Entities { get; set; }
        public Type EntityType { get; protected set; }

        public FieldAttributeCollection Fields { get; private set; }
        public ReferenceAttributeCollection References { get; private set; }

        public EntityAttribute EntityAttribute { get; set; }
        public EntityCreatorDelegate CreateProxy { get; set; }

        public string FullyQualifyFieldName(string fieldName)
        {
            return string.Format("[{0}].{1}", EntityAttribute.NameInStore, fieldName);
        }

        public string AliasFieldName(string fieldName)
        {
            return string.Format("{0}{1}", EntityAttribute.NameInStore, fieldName);
        }

        public IEntitySerializer GetSerializer()
        {
            if (_serializer != null)
                return _serializer;

            if (EntityAttribute.Serializer != null)
            {
                var constructor = EntityAttribute.Serializer.GetConstructors().First(GetConstructWithIEntityInfoParam);
                _serializer = constructor.Invoke(new object[] {this}) as IEntitySerializer;
            }

            return _serializer ?? new DefaultEntitySerializer(this);
        }

        public ReferenceAttribute GetReference(Type refType)
        {
            return References.FirstOrDefault(_ => _.ReferenceEntityType == refType);
        }

        private static bool GetConstructWithIEntityInfoParam(ConstructorInfo constructor)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != 1)
                return false;

            return parameters[0].ParameterType == typeof(IEntityInfo);
        }

        public string EntityName 
        {
            get
            {
                return EntityAttribute.NameInStore;
            }
            internal set
            {
                EntityAttribute.NameInStore = value;
            }
        }

        public override string ToString()
        {
            return EntityName;
        }

        public void AddField(FieldAttribute field)
        {
            Fields.Add(field);
        }

        public static IEntityInfo Create(EntityInfoCollection entities, Type entityType, IDbTypeConverter dbTypeConverter)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");

            // already added
            var entityName = entities.GetNameForType(entityType);
            if (entityName != null)
                return entities[entityName];

            var attr = entityType.GetCustomAttributes(true)
                                 .FirstOrDefault(a => a.GetType() == typeof(EntityAttribute)) as EntityAttribute;
            
            if (attr == null)
                throw new ArgumentException(string.Format("Type '{0}' does not have an EntityAttribute", entityType.Name));

            // store the NameInStore if not explicitly set
            if (attr.NameInStore == null)
                attr.NameInStore = entityType.Name;

            //TODO: validate NameInStore
            var result = new SqlEntityInfo
            {
                Entities = entities,
                EntityAttribute = attr,
                EntityType = entityType
            };

            entities.Add(result);

            // see if we have any entity 
            // get all field definitions
            foreach (var prop in GetProperties(entityType))
            {
                result.AnalyseAttribute(prop, dbTypeConverter);
            }
            
            if (result.Fields.Count == 0)
            {
                throw new EntityDefinitionException(result.EntityName, string.Format("Entity '{0}' Contains no Field definitions.", result.EntityName));
            }

            //Update Reference atribute with new known type
            foreach (var entityInfo in entities)
            {
                var entity = entityInfo as EntityInfo;
                if (entity == null)
                    continue;

                entity.UpdateRefenceAttribute();
            }

            // store a creator proxy delegate if the entity supports it (*way* faster for Selects)
            var methodInfo = entityType.GetMethod("ORM_CreateProxy", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (methodInfo != null)
            {
                result.CreateProxy = (EntityCreatorDelegate)Delegate.CreateDelegate(typeof(EntityCreatorDelegate), null, methodInfo);
            }

            return result;
        }

        private static IEnumerable<PropertyInfo> GetProperties(Type entityType)
        {
            return entityType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }

        private void UpdateRefenceAttribute()
        {
            foreach (var reference in References)
            {
                if (reference.EntityReference != null)
                    continue;

                var entityName = Entities.GetNameForType(reference.ReferenceEntityType);
                if (entityName == null)
                    continue;

                reference.EntityReference = Entities[entityName];
            }
        }

        private void AnalyseAttribute(PropertyInfo prop, IDbTypeConverter dbTypeConverter)
        {
            var sqlAttribute = GetEntityFieldAttribute(prop);
            TreatFieldAttribute(prop, sqlAttribute as FieldAttribute, dbTypeConverter);
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

            referenceAttribute.PropertyInfo = prop;
            referenceAttribute.Entity = this;
            References.Add(referenceAttribute);
        }

        private void TreatFieldAttribute(PropertyInfo prop, FieldAttribute fieldAttribute, IDbTypeConverter dbTypeConverter)
        {
            if (fieldAttribute == null)
                return;

            fieldAttribute.PropertyInfo = prop;
            fieldAttribute.Entity = this;

            if (fieldAttribute.FieldName == null)
            {
                fieldAttribute.FieldName = prop.Name;
            }
            fieldAttribute.FullFieldName = FullyQualifyFieldName(fieldAttribute.FieldName);
            fieldAttribute.AliasFieldName = AliasFieldName(fieldAttribute.FieldName);

            if (!fieldAttribute.DataTypeIsValid)
            {
                fieldAttribute.DataType = dbTypeConverter.ToDbType(prop.PropertyType);
            }

            if (!Fields.ContainsField(fieldAttribute.FieldName))
            {
                Fields.Add(fieldAttribute);
            }
        }
    }
}
