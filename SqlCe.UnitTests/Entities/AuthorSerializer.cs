using System;
using System.Data;
using Orm.Core;
using Orm.Core.Interfaces;

namespace Orm.SqlCe.UnitTests.Entities
{
    public class AuthorSerializer : IEntitySerializer
    {
        public IEntityInfo Entity { get; set; }
        public IEntityCache EntityCache { get; set; }

        public AuthorSerializer(IEntityInfo entity)
        {
            Entity = entity;
        }

        public bool UseFullName { get; set; }

        public object Deserialize(IDataRecord dbResult)
        {
            var item = new Author();

            for (int i = 0; i < Entity.Fields.Count; i++)
            {
                var field = Entity.Fields[i];
                var value = dbResult[UseFullName ? field.AliasFieldName : field.FieldName];
                // ReSharper disable once UnusedVariable
                var val = dbResult[i];

                switch (field.FieldName)
                {
                    case "Name":
                        item.Name = value == DBNull.Value ? null : (string)value;
                        break;
                        // fill in any additional properties here
                }
            }

            return item;
        }

        public object PopulateFields(object item, IDataRecord dbResult)
        {
            throw new NotImplementedException();
        }

        public void FillReference<TRefToFill>(object item, IDataRecord dbResult)
        {
            throw new NotImplementedException();
        }

        public void FillReference(Type refToFill, object item, IDataRecord dbResult)
        {
            throw new NotImplementedException();
        }

        public object SerializeObjectField(string fieldName, object value)
        {
            throw new NotImplementedException();
        }

        public object DeserializeObjectField(string fieldName, object value)
        {
            throw new NotImplementedException();
        }
    }
}