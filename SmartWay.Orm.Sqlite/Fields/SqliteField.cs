using SmartWay.Orm.Entity.Fields;

namespace SmartWay.Orm.Sqlite.Fields
{
    public abstract class SqliteField : FieldProperties
    {
        public override string PrimaryKeyConstraint(string constraintName, string fieldName)
        {
            return string.Empty;
        }
    }
}