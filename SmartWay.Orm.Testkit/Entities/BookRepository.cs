using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Repositories;

namespace SmartWay.Orm.Testkit.Entities
{
    public class BookRepository : Repository<Book, Book>
    {
        public BookRepository(IDataStore dataStore) : base(dataStore)
        {
        }
    }
}