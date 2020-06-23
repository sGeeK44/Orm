using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity.Constraints;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity(Serializer = typeof(AuthorSerializer))]
    public class Author
    {
        public const string PrimaryKeyName = "Id";

        [PrimaryKey(KeyScheme.Identity, Indexes = new[] {"MonIndex"})]
        public int Id { get; set; }

        [Reference(typeof(Book), Book.AuthorIdColName, PrimaryKeyName)]
        public Book[] Books { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending, Indexes = new[] {"MonIndex"})]
        public string Name { get; set; }
    }
}