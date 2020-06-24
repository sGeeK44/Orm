namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Encapsulate behaviour to manage subtract operation
    /// </summary>
    public class NegateOperation : Condition
    {
        private const string Subtract = "-";

        public NegateOperation(IFilterFactory filterFactory, IFilter part)
            : base(filterFactory, new DummyCondition(), part, Subtract)
        {
        }
    }
}