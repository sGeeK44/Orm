using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity]
    public class IndexedClass
    {
        [PrimaryKey(KeyScheme.Identity, Indexes = new[] {"MonIndex"})]
        public int Id { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending, Indexes = new[] {"MonIndex"})]
        public string Searchable { get; set; }

        [Field(RequireUniqueValue = true, Indexes = new[] {"MonIndex"})]
        public string Unique { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending, RequireUniqueValue = true, Indexes = new[] {"MonIndex"})]
        public string SearchableAndUnique { get; set; }
    }
}