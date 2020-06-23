using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity;
using SmartWay.Orm.Entity.References;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.Testkit.Entities
{
    [Entity]
    public class BookVersion : EntityBase<BookVersion>
    {
        public const string BookIdColName = "BookId";
        private readonly NullableReferenceHolder<Book> _book;

        public BookVersion() : this(null)
        {
        }

        public BookVersion(IRepository<Book> repo)
        {
            _book = new NullableReferenceHolder<Book>(repo);
        }

        [ForeignKey(typeof(Book))]
        public long? BookId
        {
            get => _book.Id;
            set => _book.Id = value;
        }

        [Reference(typeof(Book), IdColumnName, BookIdColName)]
        public Book Book
        {
            get => _book.Object;
            set => _book.Object = value;
        }
    }
}