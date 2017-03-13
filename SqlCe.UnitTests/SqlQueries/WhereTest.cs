using System.Collections.Generic;
using System.Data;
using Moq;
using NUnit.Framework;
using Orm.Core;
using Orm.Core.Filters;
using Orm.Core.Interfaces;
using Orm.Core.SqlQueries;
using Orm.SqlCe.UnitTests.Entities;

// ReSharper disable MergeConditionalExpression

namespace Orm.SqlCe.UnitTests.SqlQueries
{
    [TestFixture]
    public class WhereTest
    {
        [SetUp]
        public void Init()
        {
            Parameter = new Mock<IDataParameter>();
            Entities = new EntityInfoCollection();
            Entity = EntityInfo.Create(Entities, typeof(Author), new DefaultDbTypeConverter());
            SqlFactory = new Mock<ISqlFactory>();
            SqlFactory.Setup(_ => _.ParameterPrefix).Returns("@");
            SqlFactory.Setup(_ => _.CreateParameter()).Returns(Parameter.Object);
        }

        public IEntityInfo Entity { get; set; }
        public EntityInfoCollection Entities { get; set; }
        public Mock<IDataParameter> Parameter { get; set; }
        public Mock<ISqlFactory> SqlFactory { get; set; }

        [Test]
        public void ToStatement_Empty_ShouldReturnExpectedSql()
        {
            var where = new Where();

            List<IDataParameter> @params;
            Assert.AreEqual(string.Empty, where.ToStatement(out @params));
            Assert.AreEqual(0, @params.Count);
        }

        [Test]
        public void ToStatement_OneFilter_ShouldReturnExpectedSqlString()
        {
            var filterFactory = new FilterFactory(SqlFactory.Object, Entities);
            var primaryKey = filterFactory.ToColumnValue(Entity, Author.PrimaryKeyName);
            var value = filterFactory.ToObjectValue(1);
            var filter = filterFactory.Equal(primaryKey, value);
            var where = new Where(filter);
            
            List<IDataParameter> @params;
            Assert.AreEqual(" WHERE [Author].Id = @p0", where.ToStatement(out @params));
            Assert.AreEqual(1, @params.Count);
        }

        [Test]
        public void ToStatement_OneFilter_ShouldReturnExpectedParam()
        {
            var filterFactory = new FilterFactory(SqlFactory.Object, Entities);
            var primaryKey = filterFactory.ToColumnValue(Entity, Author.PrimaryKeyName);
            var value = filterFactory.ToObjectValue(1);
            var filter = filterFactory.Equal(primaryKey, value);
            var where = new Where(filter);
            
            List<IDataParameter> @params;
            where.ToStatement(out @params);

            Assert.AreEqual(1, @params.Count);
        }
    }
}
