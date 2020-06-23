using System;
using NUnit.Framework;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Sql.Schema;
using SmartWay.Orm.Testkit;

namespace SmartWay.Orm.Sqlite.UnitTests.Entity
{
    [TestFixture]
    public class TableDefinitionTest : DatastoreForTest
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void CleanUp()
        {
            base.CleanUp();
        }

        [Entity]
        public class EntityDefinition : EntityBase<EntityDefinition>
        {
            [Field(AllowsNulls = false)] public bool BooleanField { get; set; }

            [Field(AllowsNulls = true)] public short? ShortField { get; set; }

            [Field] public int IntegerField { get; set; }

            [Field] public long LongField { get; set; }

            [Field] public float FloatField { get; set; }

            [Field] public double DoubleField { get; set; }

            [Field(Precision = 1, Scale = 1)] public decimal DecimalField { get; set; }

            [Field(DefaultValue = "1970-01-01 00:00:00.000")]
            public DateTime DateTimeField { get; set; }

            [Field(Length = 1)] public string StringField { get; set; }

            public static TableDefinition TableDefinition
            {
                get
                {
                    var expectedTableDefinition = new TableDefinition("EntityDefinition");
                    expectedTableDefinition.AddColumn("id", 0, false, "INTEGER");
                    expectedTableDefinition.AddColumn("guid", 1, false, "TEXT");
                    expectedTableDefinition.AddColumn("BooleanField", 2, false, "INTEGER");
                    expectedTableDefinition.AddColumn("ShortField", 3, true, "INTEGER");
                    expectedTableDefinition.AddColumn("IntegerField", 4, true, "INTEGER");
                    expectedTableDefinition.AddColumn("LongField", 5, true, "INTEGER");
                    expectedTableDefinition.AddColumn("FloatField", 6, true, "REAL");
                    expectedTableDefinition.AddColumn("DoubleField", 7, true, "REAL");
                    expectedTableDefinition.AddColumn("DecimalField", 8, true, "NUMERIC");
                    expectedTableDefinition.AddColumn("DateTimeField", 9, true, "TEXT");
                    expectedTableDefinition.AddColumn("StringField", 10, true, "TEXT");
                    return expectedTableDefinition;
                }
            }
        }

        protected override void AddTypes()
        {
            DataStore.AddType<EntityDefinition>();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }

        [Test]
        public void CreateTableDefinition()
        {
            var schemaChecker = new SqliteSchemaChecker(DataStore);
            var tableDefinition = schemaChecker.GetTableFormat("EntityDefinition");

            Assert.AreEqual(EntityDefinition.TableDefinition, tableDefinition);
        }
    }
}