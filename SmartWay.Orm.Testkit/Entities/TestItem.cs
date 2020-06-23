using System;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity]
    public class TestItem : IEquatable<TestItem>
    {
        public TestItem()
        {
        }

        public TestItem(string name)
        {
            Name = name;
        }

        [PrimaryKey(KeyScheme.Identity)] public int Id { get; set; }

        [Field] public string Name { get; set; }

        [Field] public Guid? UUID { get; set; }

        [Field] public int ITest { get; set; }

        [Field] public string Address { get; set; }

        [Field] public float FTest { get; set; }

        [Field] public double DBTest { get; set; }

        [Field(Scale = 2)] public decimal DETest { get; set; }

        [Field(Length = int.MaxValue)] public string BigString { get; set; }

        [Field(FieldName = "Data")] public DateTime TestDate { get; set; }

        [Field] public DateTime? NullableTestDate { get; set; }

        [Field] public TimeSpan TestTime { get; set; }

        [Field] public TimeSpan? NullableTime { get; set; }

        [Field] public TestEnum EnumField { get; set; }

        public bool Equals(TestItem other)
        {
            return Id == other.Id;
        }
    }
}