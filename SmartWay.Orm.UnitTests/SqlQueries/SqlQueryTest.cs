using System.Data;
using Moq;
using NUnit.Framework;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;
using SmartWay.Orm.Sql.Queries;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.UnitTests.SqlQueries
{
    [TestFixture]
    public class SqlQueryTest
    {
        [SetUp]
        public void Init()
        {
            var entityCollection = new EntityInfoCollection();
            var factory = new Mock<IFieldPropertyFactory>();
            EntityInfo.Create(factory.Object, entityCollection, typeof(Author));
            EntityInfo.Create(factory.Object, entityCollection, typeof(Book));
            BookVersionEntityInfo = EntityInfo.Create(factory.Object, entityCollection, typeof(BookVersion));

            var dataStore = new Mock<IDataStore>();
            var sqlFactory = new Mock<ISqlFactory>();
            var param = new Mock<IDataParameter>();
            sqlFactory.Setup(_ => _.CreateParameter()).Returns(param.Object);
            dataStore.Setup(_ => _.SqlFactory).Returns(sqlFactory.Object);
            SqlQuery = new Selectable<Author>(dataStore.Object, entityCollection);
        }

        private const string Select = "FROM [Author]";
        private const string SelectJoin = "FROM [Author] JOIN [Book] ON [Author].[Id] = [Book].[AuthorId]";

        private const string SelectJoinChain =
            "FROM [Author] JOIN [Book] ON [Author].[Id] = [Book].[AuthorId] JOIN [BookVersion] ON [Book].[id] = [BookVersion].[BookId]";

        public IEntityInfo BookVersionEntityInfo { get; set; }

        public Selectable<Author> SqlQuery { get; set; }

        [Test]
        public void ToStatement_SelectAll_ShouldReturnExpectedSqlString()
        {
            Assert.AreEqual(Select + ";", SqlQuery.ToStatement(null));
        }

        [Test]
        public void ToStatement_SelectJoin_ShouldReturnExpectedSqlString()
        {
            SqlQuery.Join<Author, Book>();
            Assert.AreEqual(SelectJoin + ";", SqlQuery.ToStatement(null));
        }

        [Test]
        public void ToStatement_SelectWithMultipleJoin_ShouldReturnExpectedSqlString()
        {
            SqlQuery.Join<Author, Book>().Join<Book, BookVersion>();
            Assert.AreEqual(SelectJoinChain + ";", SqlQuery.ToStatement(null));
        }

        [Test]
        public void ToStatement_SelectWithWhere_ShouldReturnExpectedSqlString()
        {
            Assert.AreEqual(Select + ";", SqlQuery.ToStatement(null));
        }

        [Test]
        public void ToStatement_SelectWithWhereAndOrderBy_ShouldReturnExpectedSqlString()
        {
            var field1 = new ColumnValue(BookVersionEntityInfo, BookVersion.IdColumnName);
            var field2 = new ColumnValue(BookVersionEntityInfo, BookVersion.GuidColumnName);

            SqlQuery.OrderBy(field1).ThenBy(field2);

            Assert.AreEqual(Select + " ORDER BY [BookVersion].[id], [BookVersion].[guid];", SqlQuery.ToStatement(null));
        }

        [Test]
        public void ToStatement_SelectWithWhereAndOrderByDesc_ShouldReturnExpectedSqlString()
        {
            var field1 = new ColumnValue(BookVersionEntityInfo, BookVersion.IdColumnName);
            var field2 = new ColumnValue(BookVersionEntityInfo, BookVersion.GuidColumnName);

            SqlQuery.OrderByDesc(field1).ThenByDesc(field2);

            Assert.AreEqual(Select + " ORDER BY [BookVersion].[id] DESC, [BookVersion].[guid] DESC;",
                SqlQuery.ToStatement(null));
        }
    }
}