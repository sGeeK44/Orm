using System.Data;

// ReSharper disable TypeParameterCanBeVariant

namespace Orm.Core.SqlQueries
{
    public interface ISelect<TIEntity> : ISqlClause
    {
        int Offset { get; set; }
        TIEntity Deserialize(IDataReader results, IEntityCache entityCache);
    }

    public interface ISelect : ISqlClause
    {
        int Offset { get; set; }
        object Deserialize(IDataReader results, IEntityCache entityCache);
    }
}