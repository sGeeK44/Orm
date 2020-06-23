namespace SmartWay.Orm.Sqlite.Fields
{
    public class StringField : SqliteField
    {
        public override string GetDataTypeDefinition()
        {
            return "TEXT";
        }
    }
}