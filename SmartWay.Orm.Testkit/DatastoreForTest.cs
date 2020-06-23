using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using SmartWay.Orm.Entity.Fields;
using SmartWay.Orm.Sql;

namespace SmartWay.Orm.Testkit
{
    public abstract class DatastoreForTest
    {
        public string DbPath { get; set; }
        public ISqlDataStore DataStore { get; set; }
        protected IFieldPropertyFactory FieldPropertyFactory { get; set; }

        [SetUp]
        public virtual void SetUp()
        {
            DbPath = GetDbName();
            BuildCleanDb(DbPath);
            FieldPropertyFactory = DataStore.SqlFactory.CreateFieldPropertyFactory();
        }

        [TearDown]
        public virtual void CleanUp()
        {
            DataStore?.Dispose();
        }

        public virtual string GetDbName()
        {
            return GetDbName(string.Empty);
        }

        public static string GetDbName(string suffixe)
        {
            var dbPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                TestContext.CurrentContext.Test.MethodName + suffixe + ".sdf");
            return dbPath;
        }

        protected virtual void AddTypes()
        {
        }

        public void BuildCleanDb(string datasource)
        {
            Debug.WriteLine($"Build Database:{datasource}.");
            DataStore = CreateStore(datasource);

            DeleteDbFile();

            AddTypes();
            DataStore.CreateStore();
        }

        public void BuildFromDb(DatastoreForTest source)
        {
            CleanUp();
            DeleteDbFile();
            CreateCopyDb(source);
            InitDataStore();
        }

        private void CreateCopyDb(DatastoreForTest source)
        {
            source.ForceCommitPendingChangeOnDisk();
            File.Copy(source.DbPath, DbPath);
        }

        private void ForceCommitPendingChangeOnDisk()
        {
            CleanUp();
            InitDataStore();
        }

        protected virtual void OnNewDatastore()
        {
        }

        private void DeleteDbFile()
        {
            if (DataStore.StoreExists)
                DataStore.DeleteStore();
        }

        private void InitDataStore()
        {
            DataStore = CreateStore(DbPath);
            AddTypes();
            OnNewDatastore();
        }

        protected abstract ISqlDataStore CreateStore(string datasource);
    }
}