using Orm.Core;
using Orm.Core.Attributes;

namespace Orm.SqlCe.UnitTests.Entities
{
    [Entity(KeyScheme.Identity, Serializer = typeof(TestTableSerializer))]
    public class TestTable
    {
        [Field(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Field(Length=200)]
        public byte[] ShortBinary { get; set; }

        [Field(Length = 20000)]
        public byte[] LongBinary { get; set; }

        [Field]
        public TestEnum EnumField { get; set; }

        [Field]
        public CustomObject CustomObject { get; set; }
    }
}
