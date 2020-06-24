namespace SmartWay.Orm.Filters
{
    /// <summary>
    ///     Encapsulate behaviour to manage subtract operation
    /// </summary>
    public class ModuloOperation : Condition
    {
        private const string Subtract = " MOD ";

        public ModuloOperation(IFilterFactory filterFactory, IFilter leftPart, IFilter rightPart)
            : base(filterFactory, leftPart, rightPart, Subtract)
        {
        }
    }
}