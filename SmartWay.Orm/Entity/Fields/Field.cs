using System.Reflection;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Entity.Fields
{
    public class Field : IDistinctable
    {
        public Field(IEntityInfo entity, PropertyInfo prop, FieldAttribute fieldAttribute)
        {
            Entity = entity;
            PropertyInfo = prop;
            if (fieldAttribute != null)
            {
                RequireUniqueValue = fieldAttribute.RequireUniqueValue;
                SearchOrder = fieldAttribute.SearchOrder;
                FieldName = fieldAttribute.FieldName ?? prop.Name;
                FullFieldName = $"[{Entity.GetNameInStore()}].[{FieldName}]";
                AliasFieldName = $"{Entity.GetNameInStore()}{FieldName}";

                IsCreationTracking = fieldAttribute.IsCreationTracking;
                IsUpdateTracking = fieldAttribute.IsUpdateTracking;
                IsDeletionTracking = fieldAttribute.IsDeletionTracking;
                IsLastSyncTracking = fieldAttribute.IsLastSyncTracking;
                IsSyncIdentifier = fieldAttribute.IsSyncIdentifier;
            }

            if (prop != null) FieldProperties = FieldPropertyFactory.Create(prop.PropertyType, fieldAttribute);
        }

        private IFieldPropertyFactory FieldPropertyFactory => Entity.FieldPropertyFactory;

        protected FieldProperties FieldProperties { get; }

        public IEntityInfo Entity { get; }

        /// <summary>
        ///     Field name in datastore
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        ///     Get fully qualified name (TableName.FieldName)
        /// </summary>
        public string FullFieldName { get; }

        /// <summary>
        ///     Get fully qualified name valid for column name select (TableNameFieldName)
        /// </summary>
        public string AliasFieldName { get; }

        /// <summary>
        ///     Indicate if field should be used by sync framework to detect inserted row (False by default)
        /// </summary>
        public bool IsCreationTracking { get; }

        /// <summary>
        ///     Indicate if field should be used by sync framework to detect updated row (False by default)
        /// </summary>
        public bool IsUpdateTracking { get; }

        /// <summary>
        ///     Indicate if field should be used by sync framework to detect row in tombstone table (False by default)
        /// </summary>
        public bool IsDeletionTracking { get; }

        /// <summary>
        ///     Indicate if field should be used by sync framework to detect updated row in lastsync (False by default)
        /// </summary>
        public bool IsLastSyncTracking { get; }

        /// <summary>
        ///     Indicate if field should be used by sync framework to detect identity (False by default)
        /// </summary>
        public bool IsSyncIdentifier { get; }

        protected PropertyInfo PropertyInfo { get; }

        public bool RequireUniqueValue { get; set; }

        public FieldSearchOrder SearchOrder { get; set; }

        public bool IsPrimaryKey => this == Entity.PrimaryKey;

        public virtual bool IsForeignKey => false;

        /// <summary>
        ///     Indicate if property can be setted or managed by sgbd
        /// </summary>
        public bool Settable => !(IsPrimaryKey && Entity.PrimaryKey.KeyScheme == KeyScheme.Identity) &&
                                FieldProperties.IsSettable;

        public virtual string Key => FieldName;

        public virtual string GetFieldDefinition()
        {
            return $"[{FieldName}] {FieldProperties.GetDefinition()}{GetFieldCreationAttributes()}";
        }

        protected virtual string GetFieldCreationAttributes()
        {
            return FieldProperties.GetFieldCreationAttributes();
        }

        public virtual string GetFieldConstraint()
        {
            return string.Empty;
        }

        public string GetFieldDefinitionSqlQuery()
        {
            return $"[{FieldName}] {GetFieldDefinition()}{GetFieldCreationAttributes()}";
        }

        public void SetEntityValue(object item, object value)
        {
            PropertyInfo.SetValue(item, value, null);
        }

        public object GetEntityValue(object item)
        {
            return PropertyInfo.GetValue(item, null);
        }

        public object Convert(object value)
        {
            return FieldProperties.Convert(value);
        }

        public static Field Create(IEntityInfo entity, PropertyInfo prop, FieldAttribute fieldAttribute)
        {
            return new Field(entity, prop, fieldAttribute);
        }

        public virtual object ToSqlValue(object item)
        {
            var instanceValue = GetEntityValue(item);
            bool needToUpdateInstance;
            var result = FieldProperties.ToSqlValue(instanceValue, out needToUpdateInstance);
            if (needToUpdateInstance)
            {
                var objectValue = Convert(result);
                SetEntityValue(item, objectValue);
            }

            return result;
        }

        public string ToSelectStatement()
        {
            return FullFieldName + " AS " + AliasFieldName;
        }

        public string ToInsertStatement()
        {
            return "[" + FieldName + "]";
        }

        public bool IsMatch(MemberInfo property)
        {
            return PropertyInfo.Name == property.Name;
        }
    }
}