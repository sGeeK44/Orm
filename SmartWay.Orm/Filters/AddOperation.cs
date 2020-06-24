namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Encapsulate behaviour to manage add operation
    /// </summary>
    public class AddOperation : Condition
    {
        private const string Subtract = " + ";

        public AddOperation(IFilterFactory filterFactory, IFilter leftPart, IFilter rightPart)
            : base(filterFactory, leftPart, rightPart, Subtract)
        {
        }
    }
}