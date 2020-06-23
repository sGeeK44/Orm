namespace SmartWay.Orm.Sqlite.Fields
{
    public class RealField : SqliteField
    {
        public override string GetDataTypeDefinition()
        {
            return "REAL";
        }

        public override object Convert(object value)
        {
            var unboxValue = (double) value;
            if (PropertyType == typeof(float))
                return base.Convert((float) unboxValue);
            return base.Convert(unboxValue);
        }
    }
}