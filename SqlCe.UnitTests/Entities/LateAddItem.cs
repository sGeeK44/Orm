using Orm.Core;
using Orm.Core.Attributes;

namespace Orm.SqlCe.UnitTests.Entities
{
    [Entity(KeyScheme = KeyScheme.Identity)]
    public class LateAddItem
    {
        [Field(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Field]
        public string Name { get; set; }
    }
}