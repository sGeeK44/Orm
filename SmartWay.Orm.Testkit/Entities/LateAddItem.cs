using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity]
    public class LateAddItem
    {
        [PrimaryKey(KeyScheme.Identity)] public int Id { get; set; }

        [Field] public string Name { get; set; }
    }
}