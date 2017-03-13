using System;
using Orm.Core;
using Orm.Core.Interfaces;

namespace Orm.SqlCe.UnitTests.Entities
{
    public class TestTableSerializer : DefaultEntitySerializer
    {
        public TestTableSerializer(IEntityInfo entity)
            : base(entity) { }

        public override object SerializeObjectField(string fieldName, object value)
        {
            if (fieldName == "CustomObject")
            {
                // This will always be true in this case since CustomObject is our only
                // Object Field.  The "if" block could be omitted, but for sample 
                // clarity I'm keeping it
                return value == null ? null : ((CustomObject)value).AsByteArray();
            }

            throw new NotSupportedException();
        }

        public override object DeserializeObjectField(string fieldName, object value)
        {
            if (fieldName == "CustomObject")
            {
                // This will always be true in this case since CustomObject is our only
                // Object Field.  The "if" block could be omitted, but for sample 
                // clarity I'm keeping it
                return new CustomObject(value as byte[]);
            }

            throw new NotSupportedException();
        }
    }
}