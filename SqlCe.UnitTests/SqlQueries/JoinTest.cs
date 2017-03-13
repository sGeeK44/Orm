using System.Data;
using Moq;
using NUnit.Framework;
using Orm.Core;
using Orm.Core.Interfaces;
using Orm.Core.SqlQueries;
using Orm.SqlCe.UnitTests.Entities;

namespace Orm.SqlCe.UnitTests.SqlQueries
{
    [TestFixture]
    public class JoinTest
    {
        private const string SelectJoin = "JOIN Book ON Author.Id = Book.AuthorId";

        [SetUp]
        public void Init()
        {
            var entityCollection = new EntityInfoCollection();
            Author = EntityInfo.Create(entityCollection, typeof(Author), new DefaultDbTypeConverter());
            Book = EntityInfo.Create(entityCollection, typeof(Book), new DefaultDbTypeConverter());
            BookVersion = EntityInfo.Create(entityCollection, typeof(BookVersion), new DefaultDbTypeConverter());

            var dataStore = new Mock<IDataStore>();
            var sqlFactory = new Mock<ISqlFactory>();
            var param = new Mock<IDataParameter>();
            sqlFactory.Setup(_ => _.CreateParameter()).Returns(param.Object);
            dataStore.Setup(_ => _.SqlFactory).Returns(sqlFactory.Object);
            SqlQuery = new Select<Author, Author>(dataStore.Object, entityCollection);
        }

        public IEntityInfo BookVersion { get; set; }

        public IEntityInfo Book { get; set; }

        public IEntityInfo Author { get; set; }

        public Select<Author, Author> SqlQuery { get; set; }

        [Test]
        public void ToStatement_JoinOnOneToMany_ShouldReturnExpectedSqlString()
        {
            var join = new Join(Author, Book);
        }
    }
}
