using System.Diagnostics;
using NUnit.Framework;
using Orm.Core;
using Orm.SqlCe.UnitTests.Entities;

// ReSharper disable UseStringInterpolation

namespace Orm.SqlCe.UnitTests
{
    [TestFixture]
    public class EntityCreationTests
    {
        public TestContext TestContext { get; set; }

        [Test]
        public void DelegatePerfTest()
        {
            const int iterations = 1000;
            var sw1 = new Stopwatch();
            var sw2 = new Stopwatch();

            var store = new SqlCeDataStore("test.sdf");
            store.AddType<TestItem>();
            store.AddType<TestItemD>();
            store.CreateOrUpdateStore();

            // populate test data
            var generator = new DataGenerator();
            var items = generator.GenerateTestItems(100);
            store.BulkInsert(items);
            foreach (var i in items)
            {
                store.Insert((TestItemD)i);
            }


            // no delegate
            sw1.Reset();
            sw1.Start();
            for (var i = 0; i < iterations; i++)
            {
                store.Select<TestItem>();
            }
            sw1.Stop();
            // with delegate
            sw2.Reset();
            sw2.Start();
            for (var i = 0; i < iterations; i++)
            {
                store.Select<TestItemD>();
            }
            sw2.Stop();

            var noDelegate = sw1.ElapsedMilliseconds;
            var withDelegate = sw2.ElapsedMilliseconds;

            // ReSharper disable once RedundantStringFormatCall
            Debug.WriteLine(string.Format("Delegate gave a {0}% improvement", ((float)(noDelegate - withDelegate) / withDelegate) * 100f));
        }

        [Test]
        public void SeekTestStatic()
        {
            var sw1 = new Stopwatch();

            var store = new SqlCeDataStore("test.sdf");
            store.AddType<SeekItem>();
            store.CreateOrUpdateStore();

            // populate test data
            var generator = new DataGenerator();
            var items = generator.GenerateSeekItems(100);
            store.BulkInsert(items);


            // no delegate
            sw1.Reset();
            sw1.Start();

            // TODO Fpe
            //var item = store.First<SeekItem>(System.Data.SqlServerCe.DbSeekOptions.BeforeEqual, "SeekField", 11);
            sw1.Stop();

            // item should have a value of 10
            //Assert.AreEqual(10, item.SeekField);

        }
    }
}
