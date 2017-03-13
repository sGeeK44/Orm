using Orm.Core;
using Orm.Core.Attributes;

namespace Orm.SqlCe.UnitTests.Entities
{
    [Entity(KeyScheme.Identity)]
    public class BookVersion
    {
        public const string BookIdColName = "BookId";

        public BookVersion()
        {
            // this is required for cascading inserts to work
            Id = -1;
            BookId = -1;
        }

        [Field(IsPrimaryKey=true)]
        public int Id { get; set; }

        [Field]
        public int BookId { get; set; }

        [Reference(typeof(Book), Book.PrimaryKeyName, BookIdColName, ReferenceType = ReferenceType.ManyToOne)]
        public Book Book { get; set; }
    }
}