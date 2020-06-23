using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity]
    public class SeekItem
    {
        [PrimaryKey(KeyScheme.Identity)] public int ID { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public int SeekField { get; set; }

        [Field] public string Data { get; set; }
    }
}