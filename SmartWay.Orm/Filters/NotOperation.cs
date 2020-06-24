namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Encapsulate behaviour to manage not operation
    /// </summary>
    public class NotOperation : Condition
    {
        private const string Subtract = "NOT ";

        public NotOperation(IFilterFactory filterFactory, IFilter part)
            : base(filterFactory, new DummyCondition(), part, Subtract)
        {
        }
    }
}