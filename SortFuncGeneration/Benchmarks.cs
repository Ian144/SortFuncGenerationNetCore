using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
//using Nito.Comparers;
using SortFuncCommon;
using static System.String;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable UnusedMember.Local

namespace SortFuncGeneration
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private List<Target> _xs;
        private readonly Consumer _consumer = new Consumer();

        //private ComparerAdaptor<Target> _generatedComparer;
        //private ComparerAdaptor<Target> _handCodedImperativeComparer;
        private ComparerAdaptor<Target> _generatedComparerFEC;
        private ComparerAdaptor<Target> _handCodedTernary;
        private ComparerAdaptor<Target> _emittedComparer;

        //private IComparer<Target> _nitoComparer;
        private IComparer<Target> _handCodedComposedFunctionsComparer;

        private IOrderedEnumerable<Target> _lazyLinqOrderByThenBy;

        private static readonly Func<Target, Target, int>[] _composedSubFuncs = { CmpIntProp1, CmpStrProp1, CmpIntProp2, CmpStrProp2 };

        [IterationSetup]
        public void Setup()
        {
            var dir = Path.Combine(
                Path.GetTempPath(),
                "targetData.data");

            var fs = new FileStream(dir, FileMode.Open, FileAccess.Read);
            _xs = ProtoBuf.Serializer.Deserialize<List<Target>>(fs);

            var sortBys = new List<SortBy>
            {
                new SortBy {PropName = "IntProp1", Ascending = true},
                new SortBy {PropName = "StrProp1", Ascending = true},
                new SortBy {PropName = "IntProp2", Ascending = true},
                new SortBy {PropName = "StrProp2", Ascending = true},
            };

            // lazy, evaluated in a benchmark and in the isValid function
            _lazyLinqOrderByThenBy = _xs
                .OrderBy(x => x.IntProp1)
                .ThenBy(x => x.StrProp1, StringComparer.Ordinal)
                .ThenBy(x => x.IntProp2)
                .ThenBy(x => x.StrProp2, StringComparer.Ordinal);

            //_generatedComparer = new ComparerAdaptor<Target>(
            //    SortFuncCompiler.MakeSortFunc<Target>(sortBys)
            //);

            _generatedComparerFEC = new ComparerAdaptor<Target>( SortFuncCompilerFEC.MakeSortFunc<Target>(sortBys) );

            _emittedComparer = new ComparerAdaptor<Target>( ILEmitGenerator.EmitSortFunc<Target>(sortBys) );

            _handCodedTernary = new ComparerAdaptor<Target>(HandCodedTernary);

            _handCodedComposedFunctionsComparer = new ComparerAdaptor<Target>(HandCodedComposedFuncs);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpIntProp1(Target p1, Target p2) => p1.IntProp1.CompareTo(p2.IntProp1);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpStrProp1(Target p1, Target p2) => CompareOrdinal(p1.StrProp1, p2.StrProp1);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpIntProp2(Target p1, Target p2) => p1.IntProp2.CompareTo(p2.IntProp2);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CmpStrProp2(Target p1, Target p2) => CompareOrdinal(p1.StrProp2, p2.StrProp2);

        private static int HandCodedComposedFuncs(Target aa, Target bb)
        {
            foreach (var func in _composedSubFuncs)
            {
                int cmp = func(aa, bb);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

        //private static int HandCoded(Target aa, Target bb)
        //{
        //    int s1 = aa.IntProp1.CompareTo(bb.IntProp1);
        //    if (s1 != 0) return s1;

        //    int s2 = CompareOrdinal(aa.StrProp1, bb.StrProp1);
        //    if (s2 != 0) return s2;

        //    int s3 = aa.IntProp2.CompareTo(bb.IntProp2);
        //    if (s3 != 0) return s3;

        //    return CompareOrdinal(aa.StrProp2, bb.StrProp2);
        //}

        private static int HandCodedTernary(Target xx, Target aa)
        {
            int tmp;
            return (tmp = xx.IntProp1.CompareTo(aa.IntProp1)) != 0
                ? tmp
                : (tmp = CompareOrdinal(xx.StrProp1, aa.StrProp1)) != 0
                    ? tmp
                    : (tmp = xx.IntProp2.CompareTo(aa.IntProp2)) != 0
                        ? tmp
                        : CompareOrdinal(xx.StrProp2, aa.StrProp2);
        }

        public bool IsValid()
        {
            Setup();

            var referenceOrdering = _lazyLinqOrderByThenBy.ToList();

            //var genSorted = _xs.OrderBy(tt => tt, _generatedComparer).ToList();
            //var hcSorted = _xs.OrderBy(tt => tt, _handCodedImperativeComparer).ToList();
            var genTernarySorted = _xs.OrderBy(m => m, _generatedComparerFEC).ToList();
            //var nitoSorted = _xs.OrderBy(m => m, _nitoComparer);
            var handCodedComposedFunctionsSorted = _xs.OrderBy(m => m, _handCodedComposedFunctionsComparer);
            var handCodedTernarySorted = _xs.OrderBy(m => m, _handCodedTernary).ToList();

            var genEmitSorted = _xs.OrderBy(m => m, _emittedComparer).ToList();

            //bool hcOk = referenceOrdering.SequenceEqual(hcSorted);
            //bool genSortedOk = referenceOrdering.SequenceEqual(genSorted);
            bool genTernaryOk = referenceOrdering.SequenceEqual(genTernarySorted);
            //bool nitoOk = referenceOrdering.SequenceEqual(nitoSorted);
            bool handCodedComposedFunctionsOk = referenceOrdering.SequenceEqual(handCodedComposedFunctionsSorted);
            bool handCodedTernaryOk = referenceOrdering.SequenceEqual(handCodedTernarySorted);

            bool emittedOk = referenceOrdering.SequenceEqual(genEmitSorted);

            for (int ctr = 0; ctr < referenceOrdering.Count; ++ctr)
            {
                var refTarget = referenceOrdering[ctr];
                var xxTarget = genEmitSorted[ctr];
                if (refTarget != xxTarget)
                    Console.WriteLine($"failure at: {ctr}");
            }

            return
                //hcOk &&
                //genSortedOk &&
                genTernaryOk &&
                //nitoOk &&
                handCodedComposedFunctionsOk &&
                handCodedTernaryOk &&
                emittedOk;
        }

        [Benchmark]
        public void ComposedFunctions()
        {
            _xs.Sort(_handCodedComposedFunctionsComparer);
        }

        [Benchmark]
        public void GeneratedFastExprComp()
        {
            _xs.Sort(_generatedComparerFEC);
        }

        [Benchmark]
        public void ILEmitted()
        {
            _xs.Sort(_emittedComparer);
        }

        [Benchmark]
        public void HandCoded()
        {
            _xs.Sort(_handCodedTernary);
        }
    }
}