using System;
using System.Text;
using SmartWay.Orm.Attributes;

namespace SmartWay.Orm.Entity.Fields
{
    public class ObjectField : FieldProperties
    {
        private readonly FieldProperties _specificFieldDefition;

        public ObjectField(IFieldPropertyFactory fieldPropertyFactory, FieldAttribute fieldAttribute)
        {
            if (fieldAttribute != null && fieldAttribute.SpecificDataType != null)
                _specificFieldDefition = fieldPropertyFactory.Create(fieldAttribute.SpecificDataType, fieldAttribute);
        }

        public override string GetDataTypeDefinition()
        {
            return _specificFieldDefition != null ? _specificFieldDefition.GetDataTypeDefinition() : "image";
        }

        public override void GetFieldCreationAttributes(StringBuilder definition)
        {
            if (_specificFieldDefition != null)
                _specificFieldDefition.GetFieldCreationAttributes(definition);
            else
                base.GetFieldCreationAttributes(definition);
        }

        public override string PrimaryKeyConstraint(string constraintName, string fieldName)
        {
            return string.Empty;
        }

        public override object Convert(object value)
        {
            if (!typeof(ISqlConverter).IsAssignableFrom(PropertyType))
                throw new DefinitionException("Object field have to implement ISqlConverter.");

            var result = (ISqlConverter) Activator.CreateInstance(PropertyType);
            result.FromSqlValue(value);
            return result;
        }

        public override object ToSqlValue(object instanceValue, out bool needToUpdateInstance)
        {
            if (instanceValue != null && instanceValue != DBNull.Value)
                instanceValue = ((ISqlConverter) instanceValue).ToSqlValue();

            return base.ToSqlValue(instanceValue, out needToUpdateInstance);
        }
    }
}