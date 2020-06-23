using System;
using System.Data;
using System.Reflection;
using SmartWay.Orm.Caches;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Entity.Serializers
{
    /// <summary>
    ///     Provide method to convert object in according with Field Attribute
    /// </summary>
    public class DefaultEntitySerializer : IEntitySerializer
    {
        public DefaultEntitySerializer(IEntityInfo entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            Entity = entity;
        }

        public IEntityInfo Entity { get; set; }
        public IEntityCache EntityCache { get; set; }

        /// <summary>
        ///     Indicate if current serializer should use full field name data column access. (Depending on select used)
        /// </summary>
        public bool UseFullName { get; set; }

        /// <summary>
        ///     Convert specified database result into specified intance type
        /// </summary>
        /// <param name="dbResult">DataReader get from select</param>
        /// <returns>Instance initialized</returns>
        public object Deserialize(IDataRecord dbResult)
        {
            if (dbResult == null)
                return null;

            var primaryKeyField = Entity.PrimaryKey;
            var primaryKeyName = GetReaderFieldName(primaryKeyField);
            var primaryKeyValue = primaryKeyField.Convert(dbResult[primaryKeyName]);

            if (primaryKeyValue == null)
                return null;

            var result = GetFromCache(primaryKeyValue);
            if (result != null)
                return result;

            result = Entity.CreateNewInstance();
            UpdateCache(result, primaryKeyValue);

            PopulateFields(result, dbResult);
            return result;
        }

        public void PopulateFields(object item, IDataRecord dbResult)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            foreach (var field in Entity.Fields)
            {
                var fieldName = GetReaderFieldName(field);
                var value = dbResult[fieldName];

                if (value == DBNull.Value)
                    continue;

                value = field.Convert(value);
                try
                {
                    field.SetEntityValue(item, value);
                }
                catch (Exception ex)
                {
                    var reason = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    throw new TargetInvocationException(
                        $"An exception occurs when entity's field was setted. Entity:{field.Entity.GetNameInStore()}. Field:{field.FieldName}. Value setted:{value}. Reason:{reason}.",
                        ex);
                }
            }
        }

        private void UpdateCache(object result, object primaryKeyValue)
        {
            EntityCache?.Cache(result, primaryKeyValue);
        }

        private object GetFromCache(object primaryKeyValue)
        {
            return EntityCache?.GetOrDefault(Entity.EntityType, primaryKeyValue);
        }

        private string GetReaderFieldName(Field field)
        {
            return UseFullName ? field.AliasFieldName : field.FieldName;
        }
    }
}