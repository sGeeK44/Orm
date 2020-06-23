using System.Collections.Generic;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Constants;
using SmartWay.Orm.Entity;
using SmartWay.Orm.Entity.References;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.Testkit.Entities
{
    public enum BookType
    {
        Fiction,
        NonFiction
    }

    [Entity]
    public class Book : EntityBase<Book>
    {
        public const string AuthorIdColName = "AuthorId";
        public const string BookTypeColName = "BookType";
        private readonly ReferenceCollectionHolder<Book, BookVersion> _versionList;

        public Book() : this(null)
        {
        }

        public Book(IRepository<BookVersion> repo)
        {
            _versionList = new ReferenceCollectionHolder<Book, BookVersion>(repo, this);
        }

        [Field] public int AuthorId { get; set; }

        [Reference(typeof(Author), Author.PrimaryKeyName, AuthorIdColName)]
        public Author Author { get; set; }

        [Field] public string Title { get; set; }


        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public BookType BookType { get; set; }

        [Field(IsRowVersion = true)] public long RowVersion { get; set; }

        [Reference(typeof(BookVersion), BookVersion.BookIdColName, IdColumnName)]
        public List<BookVersion> BookVersions
        {
            get => _versionList.ObjectCollection;
            set => _versionList.ObjectCollection = value;
        }
    }
}