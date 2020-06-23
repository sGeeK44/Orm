using System;

namespace SmartWay.Orm.Attributes
{
    public class ForeignKeyAttribute : FieldAttribute
    {
        public ForeignKeyAttribute(Type foreignType)
        {
            ForeignType = foreignType;
            IsForeignKey = true;
        }

        public Type ForeignType { get; set; }
    }
}