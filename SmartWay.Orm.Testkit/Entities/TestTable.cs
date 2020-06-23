﻿using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity]
    public class TestTable
    {
        [PrimaryKey(KeyScheme.Identity)] public int Id { get; set; }

        [Field(Length = 200)] public byte[] ShortBinary { get; set; }

        [Field(Length = 20000)] public byte[] LongBinary { get; set; }

        [Field] public TestEnum EnumField { get; set; }

        [Field] public CustomObject CustomObject { get; set; }
    }
}