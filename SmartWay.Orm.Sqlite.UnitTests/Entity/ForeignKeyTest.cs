using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmartWay.Orm.Attributes;
using SmartWay.Orm.Entity;
using SmartWay.Orm.Entity.References;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Repositories;
using SmartWay.Orm.Sql;
using SmartWay.Orm.Testkit;
using SmartWay.Orm.Testkit.Entities;

namespace SmartWay.Orm.Sqlite.UnitTests.Entity
{
    [TestFixture]
    public class ForeignKeyTest : DatastoreForTest
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

        protected override void AddTypes()
        {
            DataStore.AddType<Book>();
            DataStore.AddType<BookVersion>();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }

        [Test]
        public void CreateTable_WithOneForeignKeyAttribute_ShouldCreateConstraint()
        {
            DataStore.Insert(new Book());
            var schemaChecker = new SqliteSchemaChecker(DataStore);
            Assert.IsTrue(schemaChecker.IsForeignKeyExist("BookVersion", "Book"));
        }
    }

    [TestFixture]
    public class TreeEntityTest : DatastoreForTest
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
        public class Tree : EntityBase<Tree>
        {
            public const string ParentIdColumnName = "ParentId";
            private readonly ReferenceCollectionHolder<Tree, Tree> _childList;
            private readonly ReferenceHolder<Tree, long?> _parent;

            public Tree() : this(null)
            {
            }

            public Tree(IRepository<Tree> repo)
            {
                _parent = new ReferenceHolder<Tree, long?>(repo);
                _childList = new ReferenceCollectionHolder<Tree, Tree>(repo, this);
            }

            [ForeignKey(typeof(Tree))]
            public long? ParentId
            {
                get => _parent.Id;
                set => _parent.Id = value;
            }

            [Reference(typeof(Tree), IdColumnName, ParentIdColumnName)]
            public Tree Parent
            {
                get => _parent.Object;
                set => _parent.Object = value;
            }

            [Reference(typeof(Tree), ParentIdColumnName, IdColumnName)]
            public List<Tree> ChildList
            {
                get => _childList.ObjectCollection;
                set => _childList.ObjectCollection = value;
            }
        }

        public class TreeRepository : Repository<Tree, Tree>
        {
            public TreeRepository(IDataStore dataStore)
                : base(dataStore)
            {
            }

            public override List<Tree> GetAllReference<TForeignEntity>(object id)
            {
                if (typeof(TForeignEntity) == typeof(Tree))
                    return GetAllTreeReference((long)id);

                return base.GetAllReference<TForeignEntity>(id);
            }

            public List<Tree> GetAllTreeReference(long id)
            {
                var condition = DataStore.Condition<Tree>(Tree.ParentIdColumnName, id,
                    FilterOperator.Equals);

                return DataStore.Select<Tree>().Where(condition).GetValues().ToList();
            }
        }

        protected override void AddTypes()
        {
            DataStore.AddType<Tree>();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }

        [Test]
        public void AccessToChild()
        {
            var parent = new Tree();
            DataStore.Insert(parent);

            var child = new Tree {Parent = parent};
            DataStore.Insert(child);

            var repo = new TreeRepository(DataStore);

            child = new Tree(repo) {Id = 2, ParentId = 1};
            Assert.AreEqual(child.Parent, parent);
        }

        [Test]
        public void AccessToParent()
        {
            var parent = new Tree();
            DataStore.Insert(parent);

            var child = new Tree {Parent = parent};
            DataStore.Insert(child);

            var repo = new TreeRepository(DataStore);

            parent = new Tree(repo) {Id = 1};
            Assert.AreEqual(parent.ChildList.Count, 1);
            Assert.AreEqual(parent.ChildList[0], child);
        }

        [Test]
        public void CreateTable_WithOneForeignKeyAttribute_ShouldCreateConstraint()
        {
            var schemaChecker = new SqliteSchemaChecker(DataStore);
            Assert.IsTrue(schemaChecker.IsForeignKeyExist("Tree", "Tree"));
        }
    }

    [TestFixture]
    public class SeveralForeignKeyTest : DatastoreForTest
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
        public class School : EntityBase<School>
        {
        }

        [Entity]
        public class Home : EntityBase<Home>
        {
        }

        [Entity]
        public class Student : EntityBase<Student>
        {
            [ForeignKey(typeof(School))] public long? SchoolId { get; set; }

            [ForeignKey(typeof(Home))] public long? HomeId { get; set; }
        }

        protected override void AddTypes()
        {
            DataStore.AddType<School>();
            DataStore.AddType<Home>();
            DataStore.AddType<Student>();
        }

        protected override ISqlDataStore CreateStore(string datasource)
        {
            return SqliteFactory.CreateStore(datasource);
        }

        [Test]
        public void CreateTable_WithOneForeignKeyAttribute_ShouldCreateConstraint()
        {
            DataStore.Insert(new School());
            var schemaChecker = new SqliteSchemaChecker(DataStore);
            Assert.IsTrue(schemaChecker.IsForeignKeyExist("Student", "Home"));
            Assert.IsTrue(schemaChecker.IsForeignKeyExist("Student", "School"));
        }
    }
}