using Moq;
using NUnit.Framework;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Sql.Queries;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.UnitTests.SqlQueries
{
    [TestFixture]
    public class CountTest
    {
        [SetUp]
        public void SetUp()
        {
            var entityCollection = new EntityInfoCollection();
            var factory = new Mock<IFieldPropertyFactory>();
            BookVersionEntityInfo = EntityInfo.Create(factory.Object, entityCollection, typeof(BookVersion));
        }

        public IEntityInfo BookVersionEntityInfo { get; set; }

        [Test]
        public void CreateColumnCount_ShouldReturnExpectedString()
        {
            var column = new ColumnValue(BookVersionEntityInfo, BookVersion.IdColumnName);

            var select = Aggregable.CreateColumnCount(column);

            Assert.AreEqual("SELECT COUNT([BookVersion].[id]) AS count", select.SelectStatement());
        }

        [Test]
        public void CreateTableCount_ShouldReturnExpectedSqlString()
        {
            var select = Aggregable.CreateTableCount();

            Assert.AreEqual("SELECT COUNT(*) AS count", select.SelectStatement());
        }

        [Test]
        public void CreateTableCount_WithSpecifiedColumn_ShouldReturnExpectedString()
        {
            var column = new ColumnValue(BookVersionEntityInfo, BookVersion.IdColumnName);

            var select = Aggregable.CreateTableCount(column);

            Assert.AreEqual("SELECT COUNT(*) AS count, [BookVersion].[id] AS BookVersionid", select.SelectStatement());
        }
    }
}