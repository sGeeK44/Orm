using Orm.Core;
using Orm.Core.Attributes;
using Orm.Core.Constants;

namespace Orm.SqlCe.UnitTests.Entities
{
    [Entity(KeyScheme.Identity, Serializer = typeof(AuthorSerializer))]
    public class Author
    {
        public const string PrimaryKeyName = "Id";

        [Field(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Reference(typeof(Book), Book.AuthorIdColName, PrimaryKeyName, Autofill = true)]
        public Book[] Books { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public string Name { get; set; }
    }
}
