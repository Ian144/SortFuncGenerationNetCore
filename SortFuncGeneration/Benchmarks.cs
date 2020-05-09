using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using static System.String;


namespace SortFuncGeneration
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private List<Target> _xs;
        private readonly Consumer _consumer = new Consumer();

        private ComparerAdaptor<Target> _generatedComparerFEC;
        private ComparerAdaptor<Target> _handCodedTernary;
        private ComparerAdaptor<Target> _ilEmittedComparer;
        private ComparerAdaptor<Target> _generatedComparer;
        private ComparerAdaptor<Target> _handCoded;
        private ComparerAdaptor<Target> _composedFunctionsComparer;

        private IOrderedEnumerable<Target> _lazyLinqOrderByThenBy;

        private static readonly Func<Target, Target, int>[] _composedSubFuncs = { CmpIntProp1, CmpStrProp1, CmpIntProp2, CmpStrProp2 };

        [GlobalSetup]
        public void Setup()
        {
            var dir = Path.Combine(Path.GetTempPath(), "targetData.data");
            var fs = new FileStream(dir, FileMode.Open, FileAccess.Read);
            _xs = ProtoBuf.Serializer.Deserialize<List<Target>>(fs);

            var sortBys = new List<SortBy>
            {
                new SortBy(true, "IntProp1"),
                new SortBy(true, "StrProp1"),
                new SortBy(true, "IntProp2"),
                new SortBy(true, "StrProp2"),
            };

            // lazy, evaluated in a benchmark and in the isValid function
            _lazyLinqOrderByThenBy = _xs
                .OrderBy(x => x.IntProp1)
                .ThenBy(x => x.StrProp1, StringComparer.Ordinal)
                .ThenBy(x => x.IntProp2)
                .ThenBy(x => x.StrProp2, StringComparer.Ordinal);

            var makeSortFunc = SortFuncCompiler.MakeSortFunc<Target>(sortBys);
            _generatedComparer = new ComparerAdaptor<Target>(makeSortFunc);
            _generatedComparerFEC = new ComparerAdaptor<Target>(SortFuncCompilerFEC.MakeSortFunc<Target>(sortBys));
            _ilEmittedComparer = new ComparerAdaptor<Target>(ILEmitGenerator.EmitSortFunc<Target>(sortBys));
            _handCodedTernary = new ComparerAdaptor<Target>(HandCodedTernary);
            _handCoded = new ComparerAdaptor<Target>(HandCoded);
            _composedFunctionsComparer = new ComparerAdaptor<Target>(ComposedFuncs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpIntProp1(Target p1, Target p2) => p1.IntProp1.CompareTo(p2.IntProp1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpStrProp1(Target p1, Target p2) => CompareOrdinal(p1.StrProp1, p2.StrProp1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpIntProp2(Target p1, Target p2) => p1.IntProp2.CompareTo(p2.IntProp2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpStrProp2(Target p1, Target p2) => CompareOrdinal(p1.StrProp2, p2.StrProp2);

        private static int ComposedFuncs(Target aa, Target bb)
        {
            foreach (var func in _composedSubFuncs)
            {
                int cmp = func(aa, bb);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

        private static int HandCoded(Target aa, Target bb)
        {
            int tmp = aa.IntProp1.CompareTo(bb.IntProp1);
            if (tmp != 0) return tmp;

            tmp = CompareOrdinal(aa.StrProp1, bb.StrProp1);
            if (tmp != 0) return tmp;

            tmp = aa.IntProp2.CompareTo(bb.IntProp2);
            if (tmp != 0) return tmp;

            return CompareOrdinal(aa.StrProp2, bb.StrProp2);
        }

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

            var genSorted = _xs.OrderBy(tt => tt, _generatedComparer).ToList();
            var genTernarySorted = _xs.OrderBy(m => m, _generatedComparerFEC).ToList();
            var handCodedComposedFunctionsSorted = _xs.OrderBy(m => m, _composedFunctionsComparer);
            var handCodedTernarySorted = _xs.OrderBy(m => m, _handCodedTernary).ToList();
            var genEmitSorted = _xs.OrderBy(m => m, _ilEmittedComparer).ToList();

            return
                referenceOrdering.SequenceEqual(genSorted) &&
                referenceOrdering.SequenceEqual(genTernarySorted) &&
                referenceOrdering.SequenceEqual(handCodedComposedFunctionsSorted) &&
                referenceOrdering.SequenceEqual(handCodedTernarySorted) &&
                referenceOrdering.SequenceEqual(genEmitSorted);
        }

        [Benchmark]
        public void Generated() => _xs.Sort(_generatedComparer);

        //[Benchmark]
        //public void GeneratedOrderBy() => _xs.OrderBy(m => m, _generatedComparer).Consume(_consumer);

        //[Benchmark]
        //public void GeneratedFastExprComp() => _xs.Sort(_generatedComparerFEC);

        //[Benchmark]
        //public void ILEmitted() => _xs.Sort(_ilEmittedComparer);

        [Benchmark]
        public void ComposedFunctions() => _xs.Sort(_composedFunctionsComparer);

        [Benchmark]
        public void HandCoded() => _xs.Sort(_handCoded);

        [Benchmark]
        public void HandCodedTernary() => _xs.Sort(_handCodedTernary);
    }
}