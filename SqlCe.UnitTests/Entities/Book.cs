using Orm.Core;
using Orm.Core.Attributes;
using Orm.Core.Constants;

namespace Orm.SqlCe.UnitTests.Entities
{
    public enum BookType
    {
        Fiction,
        NonFiction
    }

    [Entity(KeyScheme.Identity)]
    public class Book
    {
        public const string PrimaryKeyName = "Id";
        public const string AuthorIdColName = "AuthorId";

        public Book()
        {
            // this is required for cascading inserts to work
            Id = -1;
        }

        [Field(IsPrimaryKey=true)]
        public int Id { get; set; }

        [Field]
        public int AuthorId { get; set; }

        [Reference(typeof(Author), Author.PrimaryKeyName, AuthorIdColName, ReferenceType = ReferenceType.ManyToOne)]
        public Author Author { get; set; }

        [Field]
        public string Title { get; set; }
        

        [Field(SearchOrder=FieldSearchOrder.Ascending)]
        public BookType BookType { get; set; }

        [Field(IsRowVersion=true)]
        public long RowVersion { get; set; }

        [Reference(typeof(BookVersion), BookVersion.BookIdColName, PrimaryKeyName, Autofill = true)]
        public BookVersion[] BookVersions { get; set; }
    }
}
