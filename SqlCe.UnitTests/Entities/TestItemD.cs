using System;
using Orm.Core;
using Orm.Core.Attributes;

namespace Orm.SqlCe.UnitTests.Entities
{
    [Entity(KeyScheme.Identity, Serializer = typeof(TestItemDSerialiser))]
    public class TestItemD : IEquatable<TestItemD>
    {
        public static explicit operator TestItemD(TestItem item)
        {
            return new TestItemD
            {
                Address = item.Address,
                BigString = item.BigString,
                DBTest = item.DBTest,
                DETest = item.DETest,
                FTest = item.FTest,
                ITest = item.ITest,
                Name = item.Name,
                UUID = item.UUID,
                TestDate = item.TestDate
            };
        }

        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

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

        //[Field(FieldName="Data")]
        [Field(FieldName = "Data")]
        public DateTime TestDate { get; set; }

        public bool Equals(TestItemD other)
        {
            return ID == other.ID;
        }
    }
}