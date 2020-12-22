using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SortFuncGeneration
{
    public static class ExprTreeSortFuncCompiler
    {
        private static readonly MethodInfo _intCompareTo = GetMethodInfo<int>(x => x.CompareTo(0));
        private static readonly MethodInfo _strCompareTo = GetMethodInfo<string>(x => string.CompareOrdinal(x, string.Empty));
        
        public static MethodInfo GetMethodInfo<T>(Expression<Func<T,int>> expression)
        {
            if (expression.Body is MethodCallExpression member)
                return member.Method;

            throw new ArgumentException("Expression is not a method", nameof(expression));
        }        
        
        public static Func<T, T, int> MakeSortFunc<T>(IList<SortDescriptor> sortDescriptors)
        {
            ParameterExpression param1Expr = Expression.Parameter(typeof(T));
            ParameterExpression param2Expr = Expression.Parameter(typeof(T));
            BlockExpression exprSd = MakeCompositeCompare(param1Expr, param2Expr, sortDescriptors);
            Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(exprSd, param1Expr, param2Expr);
            return lambda.Compile();
        }

        private static BlockExpression MakePropertyCompareBlock(
            SortDescriptor sortDescriptor,
            ParameterExpression propExp1,
            ParameterExpression propExp2,
            LabelTarget labelReturn,
            ParameterExpression result)
        {
            MemberExpression propA = Expression.Property(propExp1, sortDescriptor.PropName);
            MemberExpression propB = Expression.Property(propExp2, sortDescriptor.PropName);
            var (prop1, prop2) = sortDescriptor.Ascending ? (propA, propB) : (propB, propA);

            Expression compareExpr = prop1.Type switch
            {
                _ when prop1.Type == typeof(string) => Expression.Call(_strCompareTo, prop1, prop2),
                _ when prop1.Type == typeof(int)    => Expression.Call(prop1, _intCompareTo, prop2),
                _                                   => throw new ApplicationException($"unsupported property type: {prop1.Type}")
            };
            
            IEnumerable<ParameterExpression> variables = new[] { result };

            IEnumerable<Expression> expressions = new Expression[]
            {
                Expression.Assign(result, compareExpr),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Constant(0), result),
                    Expression.Goto(labelReturn, result))
            };

            return Expression.Block(variables, expressions);
        }

        private static BlockExpression MakeCompositeCompare(ParameterExpression param1Expr, ParameterExpression param2Expr, IEnumerable<SortDescriptor> sortBys)
        {
            ParameterExpression result = Expression.Variable(typeof(int), "result");
            LabelTarget labelReturn = Expression.Label(typeof(int));
            LabelExpression labelExpression = Expression.Label(labelReturn, result);
            IEnumerable<Expression> compareBlocks = sortBys.Select(propName => MakePropertyCompareBlock(propName, param1Expr, param2Expr, labelReturn, result));
            return Expression.Block(new[] { result }, compareBlocks.Append(labelExpression));
        }
    }
}
