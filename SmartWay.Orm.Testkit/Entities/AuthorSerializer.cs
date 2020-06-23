using System;
using System.Data;
using SmartWay.Orm.Caches;
using SmartWay.Orm.Entity.Serializers;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Testkit.Entities
{
    public class AuthorSerializer : IEntitySerializer
    {
        public AuthorSerializer(IEntityInfo entity)
        {
            Entity = entity;
        }

        public IEntityInfo Entity { get; set; }
        public IEntityCache EntityCache { get; set; }

        public bool UseFullName { get; set; }

        public object Deserialize(IDataRecord dbResult)
        {
            var item = new Author();

            for (var i = 0; i < Entity.Fields.Count; i++)
            {
                var field = Entity.Fields[i];
                var value = dbResult[UseFullName ? field.AliasFieldName : field.FieldName];
                // ReSharper disable once UnusedVariable
                var val = dbResult[i];

                switch (field.FieldName)
                {
                    case "Name":
                        item.Name = value == DBNull.Value ? null : (string) value;
                        break;
                    // fill in any additional properties here
                }
            }

            return item;
        }

        public void PopulateFields(object item, IDataRecord dbResult)
        {
            throw new NotImplementedException();
        }
    }
}