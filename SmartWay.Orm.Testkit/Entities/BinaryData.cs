using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity]
    public class BinaryData
    {
        [PrimaryKey(KeyScheme.Identity, SearchOrder = FieldSearchOrder.Ascending)]
        public int ID { get; set; }

        [Field] public byte[] BinaryField { get; set; }

        [Field] public byte[] ImageField { get; set; }

        [Field(Length = 2009)] public string NTextField { get; set; }
    }
}