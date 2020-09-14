using System.Collections.Generic;
using System.Linq;
using SmartWay.Orm.Filters;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.Testkit.Entities
{
    public class BookVersionRepository : Repository<BookVersion, BookVersion>
    {
        public BookVersionRepository(IDataStore dataStore)
            : base(dataStore)
        {
        }

        public override List<BookVersion> GetAllReference<TForeignEntity>(object id)
        {
            if (typeof(TForeignEntity) == typeof(Book))
                return GetAllBookReference((long)id);

            return base.GetAllReference<TForeignEntity>(id);
        }

        public List<BookVersion> GetAllBookReference(long id)
        {
            var condition = DataStore.Condition<BookVersion>(BookVersion.BookIdColName, id,
                FilterOperator.Equals);

            return DataStore.Select<BookVersion>().Where(condition).GetValues().ToList();
        }
    }
}