namespace SmartWay.Orm.Sqlite.Fields
{
    public class BinaryField : SqliteField
    {
        public override string GetDataTypeDefinition()
        {
            return "BLOB";
        }
    }
}