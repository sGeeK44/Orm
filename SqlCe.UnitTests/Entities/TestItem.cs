using System;
using Orm.Core;
using Orm.Core.Attributes;

namespace Orm.SqlCe.UnitTests.Entities
{
    [Entity(KeyScheme = KeyScheme.Identity)]
    public class TestItem : IEquatable<TestItem>
    {
        public TestItem()
        {
        }

        public TestItem(string name)
        {
            Name = name;
        }

        [Field(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Field]
        public string Name { get; set; }

        [Field]
        public Guid? UUID { get; set; }

        [Field]
        public int ITest { get; set; }

        [Field]
        public string Address { get; set; }

        [Field]
        public float FTest { get; set; }

        [Field]
        public double DBTest { get; set; }

        [Field(Scale = 2)]
        public decimal DETest { get; set; }

        [Field(Length = int.MaxValue)]
        public string BigString { get; set; }

        [Field(FieldName="Data")]
        public DateTime TestDate { get; set; }

        public bool Equals(TestItem other)
        {
            return Id == other.Id;
        }
    }
}
