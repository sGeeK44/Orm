using System;
using System.Data;
using Orm.Core.Attributes;
using Orm.Core.Entity;
using Orm.Core.Interfaces;

// ReSharper disable UseNameofExpression
// ReSharper disable UseStringInterpolation

namespace Orm.Core
{
    /// <summary>
    /// Provide method to convert object in according with Field Attribute
    /// </summary>
    public class DefaultEntitySerializer :  IEntitySerializer
    {
        public IEntityInfo Entity { get; set; }
        public IEntityCache EntityCache { get; set; }

        public DefaultEntitySerializer(IEntityInfo entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            Entity = entity;
        }

        /// <summary>
        /// Indicate if current serializer should use full field name data column access. (Depending on select used)
        /// </summary>
        public bool UseFullName { get; set; }

        /// <summary>
        /// Convert specified database result into specified intance type
        /// </summary>
        /// <param name="dbResult">DataReader get from select</param>
        /// <returns>Instance initialized</returns>
        public object Deserialize(IDataRecord dbResult)
        {
            object result = null;
            Diagnostics.Measure(string.Format("Build {0} entity from reader", Entity.EntityName), () =>
            {
                var primaryKeyField = Entity.Fields.KeyField;
                var primaryKeyName = GetReaderFieldName(primaryKeyField);
                var primaryKeyValue = Convert(primaryKeyField, dbResult[primaryKeyName]);

                if (primaryKeyValue == DBNull.Value)
                    return;

                result = GetFromCache(primaryKeyValue);
                if (result != null)
                    return;

                result = Activator.CreateInstance(Entity.EntityType);
                UpdateCache(result, primaryKeyValue);

                PopulateFields(result, dbResult);
            });
            return result;
        }

        private void UpdateCache(object result, object primaryKeyValue)
        {
            if (EntityCache != null)
                EntityCache.Add(result, primaryKeyValue);
        }

        private object GetFromCache(object primaryKeyValue)
        {
            return EntityCache != null
                 ? EntityCache.Get(Entity.EntityType, primaryKeyValue)
                 : null;
        }

        private string GetReaderFieldName(FieldAttribute field)
        {
            return UseFullName ? field.AliasFieldName : field.FieldName;
        }

        public object PopulateFields(object item, IDataRecord dbResult)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            object primaryKey = null;
            foreach (var field in Entity.Fields)
            {
                var fieldName = GetReaderFieldName(field);
                var value = dbResult[fieldName];

                if (field.IsPrimaryKey)
                    primaryKey = value;

                if (value == DBNull.Value)
                    continue;

                value = Convert(field, value);
                field.PropertyInfo.SetValue(item, value, null);
            }

            return primaryKey;
        }

        public void FillReference<TRefToFill>(object item, IDataRecord dbResult)
        {
            FillReference(typeof(TRefToFill), item, dbResult);
        }

        public void FillReference(Type refToFill, object item, IDataRecord dbResult)
        {
            if (item == null) return;

            var referenceToFill = Entity.GetReference(refToFill);
            if (referenceToFill == null)
                return;
            
            var serialize = referenceToFill.GetReferenceSerializer();
            serialize.UseFullName = UseFullName;
            serialize.EntityCache = EntityCache;
            var refenceValue = serialize.Deserialize(dbResult);
            referenceToFill.SetValue(item, refenceValue);
        }

        public virtual object SerializeObjectField(string fieldName, object value)
        {
            throw new NotSupportedException(string.Format("Default entity serializer do not take in charge object field serialization. Please implement your own for {0} field.", fieldName));
        }

        public virtual object DeserializeObjectField(string fieldName, object value)
        {
            throw new NotSupportedException(string.Format("Default entity serializer do not take in charge object field deserialization. Please implement your own for {0} field.", fieldName));
        }

        private object Convert(FieldAttribute field, object value)
        {
            if (typeof(ICustomSqlField).IsAssignableFrom(field.PropertyInfo.PropertyType))
            {
                var result = Activator.CreateInstance(field.PropertyInfo.PropertyType) as ICustomSqlField;
                result.FromSqlValue(value);
                return result;
            }
            if (field.DataType == DbType.Object)
            {
                return DeserializeObjectField(field.FieldName, value);
            }
            if (field.IsRowVersion)
            {
                // sql stores this an 8-byte array
                return BitConverter.ToInt64((byte[])value, 0);
            }
            if (field.IsTimespan)
            {
                // SQL Compact doesn't support Time, so we're convert to ticks in both directions
                return new TimeSpan((long) value);
            }
            var propType = field.PropertyInfo.PropertyType;
            if (propType.IsNullable())
            {
                var args = propType.GetGenericArguments();
                if (args.Length != 1)
                    throw new NotSupportedException(string.Format("Converter doesn't support this type of nullable. Type:{0}", field.PropertyInfo.PropertyType));

                if (args[0].IsEnum)
                    return Enum.ToObject(args[0], value);
            }
            return value;
        }
    }
}