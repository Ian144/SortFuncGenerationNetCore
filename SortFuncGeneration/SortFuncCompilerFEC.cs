using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using static System.Linq.Expressions.Expression;
// ReSharper disable PossibleMultipleEnumeration

namespace SortFuncGeneration
{
    public static class SortFuncCompilerFEC
    {
        private static readonly MethodInfo _strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
        private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });
        private static readonly ConstantExpression _zeroExpr = Constant(0);

        private static Expression MakePropertyCompareExpressionCall(SortBy sortDescriptor, ParameterExpression rm1, ParameterExpression rm2){
            var propA = Property(rm1, sortDescriptor.PropName);
            var propB = Property(rm2, sortDescriptor.PropName);
            var (prop1, prop2) = sortDescriptor.Ascending ? (propA, propB) : (propB, propA);
            if (prop1.Type == typeof(string)) {
                return Call(_strCompareOrdinal, prop1, prop2);
            }
            if (prop1.Type == typeof(int)) {
                return Call(prop1, _intCompareTo, prop2);
            }

            throw new ApplicationException($"comparison not supported for: {prop1.Type}");
        }

        private static Expression MakeSortExpression<T>(IEnumerable<SortBy> sortBys, ParameterExpression param1Expr, ParameterExpression param2Expr, ParameterExpression tmpInt) {
            if (sortBys.Count() == 1) {
                return MakePropertyCompareExpressionCall(sortBys.First(), param1Expr, param2Expr);
            }
            var compare = MakePropertyCompareExpressionCall(sortBys.First(), param1Expr, param2Expr);
            return Condition(
                NotEqual(Assign(tmpInt, compare), _zeroExpr), // assign the value to tmpInt and perform the comparison, assignment is an expressions and has a value
                tmpInt,
                MakeSortExpression<T>(sortBys.Skip(1), param1Expr, param2Expr, tmpInt)
            );
        }

        public static Func<T, T, int> MakeSortFunc<T>(IList<SortBy> sortBys){
            var param1 = Parameter(typeof(T));
            var param2 = Parameter(typeof(T));
            var tmpInt = Variable(typeof(int));
            Expression compositeCompare = MakeSortExpression<T>(sortBys, param1, param2, tmpInt);
            var block = Block(tmpInt, compositeCompare);
            var lambda = Lambda<Func<T, T, int>>(block, param1, param2);
            return lambda.CompileFast(true);
        }
    }
}

