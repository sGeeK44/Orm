namespace SmartWay.Orm.Sql.Queries
{
    public class SelectTop<TIEntity> : Select<TIEntity>
        where TIEntity : class
    {
        private readonly int _quantity;

        public SelectTop(Selectable<TIEntity> selectable, int quantity)
            : base(selectable)
        {
            _quantity = quantity;
        }

        protected override string SelectVerb => $"{base.SelectVerb}TOP({_quantity}) ";
    }
}