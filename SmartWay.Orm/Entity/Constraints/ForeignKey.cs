using System;
using System.Reflection;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Interfaces;

namespace SmartWay.Orm.Entity.Constraints
{
    public class ForeignKey : Field
    {
        private readonly Lazy<string> _constraintName;

        public ForeignKey(EntityInfoCollection entities, IEntityInfo entityLocal, PropertyInfo prop,
            ForeignKeyAttribute foreignKeyAttribute)
            : base(entityLocal, prop, foreignKeyAttribute)
        {
            _constraintName = new Lazy<string>(ComputeConstraintName);
            Entities = entities;
            ForeignType = foreignKeyAttribute.ForeignType;
        }

        private EntityInfoCollection Entities { get; }

        public string ConstraintName => _constraintName.Value;

        public Type ForeignType { get; set; }

        public IEntityInfo ForeignEntityInfo => Entities[ForeignType];

        public override bool IsForeignKey => true;

        public string GetCreateSqlQuery()
        {
            return $"ALTER TABLE [{Entity.GetNameInStore()}] ADD {GetFieldConstraint()};";
        }

        public override string GetFieldConstraint()
        {
            return
                $"CONSTRAINT {ConstraintName} FOREIGN KEY ({FieldName}) REFERENCES [{ForeignEntityInfo.GetNameInStore()}]({ForeignEntityInfo.PrimaryKey.FieldName})";
        }

        public static ForeignKey Create(EntityInfoCollection entities, IEntityInfo entityLocal, PropertyInfo prop,
            ForeignKeyAttribute foreignKeyAttribute)
        {
            return new ForeignKey(entities, entityLocal, prop, foreignKeyAttribute);
        }

        private string ComputeConstraintName()
        {
            var entityForeignName = Entities.GetNameForType(ForeignType);
            return $"ORM_FK_{Entity.GetNameInStore()}_{entityForeignName}";
        }
    }
}