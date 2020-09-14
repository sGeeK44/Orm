using System;
using System.Reflection;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Entity.Constraints
{
    public class PrimaryKey : Field
    {
        private readonly Lazy<string> _constraintName;

        private PrimaryKey(IEntityInfo entity, PropertyInfo prop, PrimaryKeyAttribute pkAttribute)
            : base(entity, prop, pkAttribute)
        {
            _constraintName = new Lazy<string>(ComputeConstraintName);
            KeyScheme = pkAttribute.KeyScheme;
            NullPkValue = GetDefaultValue(PropertyInfo.PropertyType);
        }

        public KeyScheme KeyScheme { get; set; }

        public string ConstraintName => _constraintName.Value;

        public object NullPkValue { get; }

        public string GetCreateSqlQuery()
        {
            return $"ALTER TABLE [{Entity.GetNameInStore()}] ADD {Constraint()};";
        }

        public string Constraint()
        {
            return $"CONSTRAINT [{ConstraintName}] PRIMARY KEY ([{FieldName}])";
        }

        private string ComputeConstraintName()
        {
            return $"ORM_PK_{Entity.GetNameInStore()}";
        }

        protected override string GetFieldCreationAttributes()
        {
            var result = base.GetFieldCreationAttributes();
            if (KeyScheme == KeyScheme.Identity) result += FieldProperties.GetIdentity();
            result += FieldProperties.PrimaryKeyConstraint(ConstraintName, FieldName);

            return result;
        }

        public override object ToSqlValue(object item)
        {
            var instanceValue = GetEntityValue(item);
            switch (KeyScheme)
            {
                case KeyScheme.GUID:
                    if (instanceValue.Equals(Guid.Empty))
                    {
                        instanceValue = Guid.NewGuid();
                        SetEntityValue(item, instanceValue);
                    }

                    break;
            }

            return instanceValue;
        }
        private object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }

        public static PrimaryKey Create(IEntityInfo entity, PropertyInfo prop, PrimaryKeyAttribute primaryKeyAttribute)
        {
            return new PrimaryKey(entity, prop, primaryKeyAttribute);
        }
    }
}