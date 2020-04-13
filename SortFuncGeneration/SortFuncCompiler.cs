using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SortFuncGeneration
{
    public static class SortFuncCompiler
    {
        private static readonly MethodInfo _strCompareTo = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
        private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });

        public static Func<T, T, int> MakeSortFunc<T>(IList<SortBy> sortDescriptors)
        {
            ParameterExpression param1Expr = Expression.Parameter(typeof(T));
            ParameterExpression param2Expr = Expression.Parameter(typeof(T));
            BlockExpression exprSd = MakeCompositeCompare(param1Expr, param2Expr, sortDescriptors);
            Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(exprSd, param1Expr, param2Expr);
            return lambda.Compile();
        }

        //public static Func<T, T, int> MakeSortFuncCompToMeth<T>(IList<SortBy> sortDescriptors)
        //{
        //    ParameterExpression param1Expr = Expression.Parameter(typeof(T));
        //    ParameterExpression param2Expr = Expression.Parameter(typeof(T));
        //    BlockExpression exprSd = MakeCompositeCompare(param1Expr, param2Expr, sortDescriptors);
        //    Expression<Func<T, T, int>> lambda = Expression.Lambda<Func<T, T, int>>(exprSd, param1Expr, param2Expr);

        //    var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("assembly"), AssemblyBuilderAccess.Run);
        //    var dynamicModule = ab.DefineDynamicModule("module");
        //    var typeBuilder = dynamicModule.DefineType("type", TypeAttributes.Public);
        //    var methodBuilder = typeBuilder.DefineMethod("test3", MethodAttributes.Public | MethodAttributes.Static);
        //    lambda.CompileToMethod(methodBuilder);
        //    var dynamicType = typeBuilder.CreateType();
        //    // ReSharper disable once AssignNullToNotNullAttribute // don't mind blowing up if dynamicType.GetMethod returns null
        //    return (Func<T, T, int>)Delegate.CreateDelegate(typeof(Func<T, T, int>), dynamicType.GetMethod("test3"));
        //}

        private static BlockExpression MakePropertyCompareBlock(
            SortBy sortDescriptor,
            ParameterExpression propExp1,
            ParameterExpression propExp2,
            LabelTarget labelReturn,
            ParameterExpression result)
        {
            MemberExpression propA = Expression.Property(propExp1, sortDescriptor.PropName);
            MemberExpression propB = Expression.Property(propExp2, sortDescriptor.PropName);
            var (prop1, prop2) = sortDescriptor.Ascending ? (propA, propB) : (propB, propA);

            Expression compareExpr;

            if (prop1.Type == typeof(string))
            {
                compareExpr = Expression.Call(_strCompareTo, prop1, prop2);
            }
            else if (prop1.Type == typeof(int))
            {
                compareExpr = Expression.Call(prop1, _intCompareTo, prop2);
            }
            else
            {
                throw new ApplicationException($"unsupported property type: {prop1.Type}");
            }

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

        private static BlockExpression MakeCompositeCompare(ParameterExpression param1Expr, ParameterExpression param2Expr, IEnumerable<SortBy> sortBys)
        {
            ParameterExpression result = Expression.Variable(typeof(int), "result");
            LabelTarget labelReturn = Expression.Label(typeof(int));
            LabelExpression labelExpression = Expression.Label(labelReturn, result);
            IEnumerable<Expression> compareBlocks = sortBys.Select(propName => MakePropertyCompareBlock(propName, param1Expr, param2Expr, labelReturn, result));
            return Expression.Block(new[] { result }, compareBlocks.Append(labelExpression));
        }
    }
}
