using System.Collections.Generic;
using System.Data;
using Moq;
using NUnit.Framework;
using Orm.Core;
using Orm.Core.Filters;
using Orm.Core.Interfaces;
using Orm.Core.SqlQueries;
using Orm.SqlCe.UnitTests.Entities;

namespace Orm.SqlCe.UnitTests.SqlQueries
{
    [TestFixture]
    public class SqlQueryTest
    {
        private const string Select = "SELECT [Author].Id AS AuthorId, [Author].Name AS AuthorName FROM [Author]";
        private const string SelectJoin = "SELECT [Author].Id AS AuthorId, [Author].Name AS AuthorName, [Book].Id AS BookId, [Book].AuthorId AS BookAuthorId, [Book].Title AS BookTitle, [Book].BookType AS BookBookType, [Book].RowVersion AS BookRowVersion FROM [Author] JOIN [Book] ON [Author].Id = [Book].AuthorId";
        private const string SelectJoinChain = "SELECT [Author].Id AS AuthorId, [Author].Name AS AuthorName, [Book].Id AS BookId, [Book].AuthorId AS BookAuthorId, [Book].Title AS BookTitle, [Book].BookType AS BookBookType, [Book].RowVersion AS BookRowVersion, [BookVersion].Id AS BookVersionId, [BookVersion].BookId AS BookVersionBookId FROM [Author] JOIN [Book] ON [Author].Id = [Book].AuthorId JOIN [BookVersion] ON [Book].Id = [BookVersion].BookId";

        [SetUp]
        public void Init()
        {
            var entityCollection = new EntityInfoCollection();
            EntityInfo.Create(entityCollection, typeof(Author), new DefaultDbTypeConverter());
            EntityInfo.Create(entityCollection, typeof(Book), new DefaultDbTypeConverter());
            EntityInfo.Create(entityCollection, typeof(BookVersion), new DefaultDbTypeConverter());

            var dataStore = new Mock<IDataStore>();
            var sqlFactory = new Mock<ISqlFactory>();
            var param = new Mock<IDataParameter>();
            sqlFactory.Setup(_ => _.CreateParameter()).Returns(param.Object);
            dataStore.Setup(_ => _.SqlFactory).Returns(sqlFactory.Object);
            SqlQuery = new Select<Author, Author>(dataStore.Object, entityCollection);
        }

        public Select<Author, Author> SqlQuery { get; set; }

        [Test]
        public void ToStatement_SelectAll_ShouldReturnExpectedSqlString()
        {
            List<IDataParameter> @params;
            Assert.AreEqual(Select + ";", SqlQuery.ToStatement(out @params));
        }

        [Test]
        public void ToStatement_SelectWithWhere_ShouldReturnExpectedSqlString()
        {
            var sqlWhereSql = SetupWhereCondition();
            List<IDataParameter> @params;
            Assert.AreEqual(Select + sqlWhereSql + ";", SqlQuery.ToStatement(out @params));
        }

        [Test]
        public void ToStatement_SelectJoin_ShouldReturnExpectedSqlString()
        {
            SqlQuery.Join<Author, Book>();
            List<IDataParameter> @params;
            Assert.AreEqual(SelectJoin + ";", SqlQuery.ToStatement(out @params));
        }

        [Test]
        public void ToStatement_SelectWithWhereAndOrderBy_ShouldReturnExpectedSqlString()
        {
            var sqlWhereSql = SetupWhereCondition();
            var sqlOrderBySql = SetupOrderByCondition();
            List<IDataParameter> @params;
            Assert.AreEqual(Select + sqlWhereSql + sqlOrderBySql + ";", SqlQuery.ToStatement(out @params));
        }

        [Test]
        public void ToStatement_SelectWithMultipleJoin_ShouldReturnExpectedSqlString()
        {
            SqlQuery.Join<Author, Book>().Join<Book, BookVersion>();
            List<IDataParameter> @params;
            Assert.AreEqual(SelectJoinChain + ";", SqlQuery.ToStatement(out @params));
        }

        private string SetupOrderByCondition()
        {
            const string entityName = "XX";
            var entity = new Mock<IEntityInfo>();
            entity.Setup(_ => _.EntityName).Returns(entityName);
            const string field1Value = "A";
            const string field2Value = "B";
            var field1 = new ColumnValue(entity.Object, field1Value);
            var field2 = new ColumnValue(entity.Object, field2Value);
            SqlQuery.OrderBy(field1, field2);
            return " ORDER BY [" + entityName + "]." + field1Value + ", [" + entityName + "]." + field2Value;
        }

        private string SetupWhereCondition()
        {
            const string sqlWhereSql = " XXXXXXXX";
            var sqlWhereClause = new Mock<ISqlClause>();
            List<IDataParameter> @params;
            sqlWhereClause.Setup(_ => _.ToStatement(out @params)).Returns(sqlWhereSql);
            SqlQuery.Where(sqlWhereClause.Object);
            return sqlWhereSql;
        }
    }
}
