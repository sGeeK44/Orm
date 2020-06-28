using System;
using System.Linq.Expressions;
using SmartWay.Orm.Queries;
using SmartWay.Orm.Sql.Queries;

namespace SmartWay.Orm.Filters
{
    public class FilterBuilder<T>
        where T : class
    {
        private readonly EntityInfoCollection _entityInfos;
        private readonly IFilterFactory _filterFactory;
        private readonly LambdaExpression _expression;

        public FilterBuilder(EntityInfoCollection entityInfos, IFilterFactory filterFactory, LambdaExpression exp)
        {
            _entityInfos = entityInfos;
            _filterFactory = filterFactory;
            _expression = exp;
        }

        public IClause Build()
        {
            var filter = ToStatement(_expression);
            return new Where(filter);
        }

        private IFilter ToStatement(Expression exp)
        {
            if (exp == null)
                return null;
            switch (exp.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return ToStatement(exp.NodeType, (UnaryExpression)exp);
                case ExpressionType.Not:
                    return ToStatement(exp.NodeType, (UnaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.NotEqual:
                    return ToStatement(exp.NodeType, (BinaryExpression)exp);
                case ExpressionType.Constant:
                    return ToStatement((ConstantExpression)exp);
                case ExpressionType.MemberAccess:
                    return ToStatement((MemberExpression)exp);
                case ExpressionType.Lambda:
                    return ToStatement((LambdaExpression)exp);
                case ExpressionType.Parameter:
                    return ToStatement((ParameterExpression)exp);
                default:
                    throw new NotSupportedException(string.Format("Not supported expression type: '{0}'", exp.NodeType));
            }
        }

        protected virtual IFilter ToStatement(ExpressionType type, UnaryExpression u)
        {
            return ToStatement(null, u.Operand, type);
        }

        protected virtual IFilter ToStatement(ExpressionType type, BinaryExpression b)
        {
            if (b.Left is ParameterExpression)
            {
                //if (type == ExpressionType.Equal)
                //    return ToStatement(b.Right);
                throw new NotImplementedException();
            }
            if (b.Right is ParameterExpression)
            {
                //if (type == ExpressionType.Equal)
                //    return ToStatement(b.Left);
                throw new NotImplementedException();
            }

            return ToStatement(b.Left, b.Right, type);
        }

        protected virtual IFilter ToStatement(ConstantExpression c)
        {
            return _filterFactory.ToObjectValue(c.Value);
        }

        protected virtual IFilter ToStatement(LambdaExpression l)
        {
            return l.Body.NodeType == ExpressionType.Constant ? null : ToStatement(l.Body);
        }

        protected virtual IFilter ToStatement(MemberExpression m)
        {
            string fieldName = null;
            if (m.Expression is ParameterExpression parameterExpression) fieldName = parameterExpression.Name;
            else if (m.Expression is MemberExpression expression) fieldName = expression.Member.Name;
            else if (m.Expression is ConstantExpression constantExpression) fieldName = constantExpression.Value.ToString();
            else if (m.Expression is UnaryExpression unary)
            {
                if (unary.Operand is ParameterExpression operand) fieldName = operand.Name;
            }
            else throw new NotSupportedException(string.Format("Type not supported. Type:{0}.", m.Expression.GetType()));

            if (IsLambdaArgument(fieldName))
            {
                return GetColumnName(m);
            }

            var objectMember = Expression.Convert(m, typeof(object));

            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            var getter = getterLambda.Compile();

            return _filterFactory.ToObjectValue(getter());
        }

        protected virtual IFilter ToStatement(ParameterExpression p)
        {
            throw new NotImplementedException();
        }

        private bool IsLambdaArgument(string fieldName)
        {
            return string.Equals(_expression.Parameters[0].Name, fieldName);
        }

        private IFilter ToStatement(Expression leftPart, Expression rightPart, ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return new NegateOperation(_filterFactory, ToStatement(rightPart));
                case ExpressionType.Not:
                    return new NotOperation(_filterFactory, ToStatement(rightPart));
                //return "NOT";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return new AddOperation(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return new SubtractOperation(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return new MultiOperation(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.Divide:
                    return new DivideOperation(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.Modulo:
                    return new ModuloOperation(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return new And(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return new Or(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.LessThan:
                    return new LessThanCondition(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.LessThanOrEqual:
                    return new LessThanOrEqualCondition(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.GreaterThan:
                    return new GreaterThanCondition(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.GreaterThanOrEqual:
                    return new GreaterThanOrEqualCondition(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.Equal:
                    return new EqualCondition(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                case ExpressionType.NotEqual:
                    return new NotEqualCondition(_filterFactory, ToStatement(leftPart), ToStatement(rightPart));
                default:
                    throw new Exception(string.Format("Unhandled expression type: '{0}'", type));
            }
        }

        public IFilter GetColumnName(MemberExpression body)
        {
            if (body == null)
                throw new ArgumentNullException("memberExpression");

            var entityName = _entityInfos.GetNameForType(typeof(T));
            var entityInfo = _entityInfos[entityName];
            var property = body.Member;
            var requestedProperty = entityInfo.GetField(property);
            if (requestedProperty == null)
                throw new NotSupportedException(string.Format("Type doesn't contains member expression property. Requested type:{0}. Property name:{1}.", typeof(T), property.Name));

            return _filterFactory.ToColumnValue(entityInfo, requestedProperty.FieldName);
        }
    }
}