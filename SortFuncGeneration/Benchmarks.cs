using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Nito.Comparers;
using static System.String;


namespace SortFuncGeneration
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private static readonly List<SortDescriptor> _sortBys = new()
        {
            new SortDescriptor(true, "IntProp1"),
            new SortDescriptor(true, "StrProp1"),
            new SortDescriptor(true, "IntProp2"),
            new SortDescriptor(true, "StrProp2"),
        };        

        private static readonly Func<Target, Target, int>[] _composedSubFuncs = { CmpIntProp1, CmpStrProp1, CmpIntProp2, CmpStrProp2 };        
        
        private static List<Target> _source;
        private static Target[] _sortTargets;
        
        private static readonly Consumer _consumer = new();

        private static readonly InlineComparer _inlineComparer                       = new ();
        private static readonly IntInlineComparer _intInlineComparer                 = new ();
        private static readonly ComparerAdaptor<Target> _handCodedTernary            = new( HandCodedTernary );
        private static readonly ComparerAdaptor<Target> _ilEmittedComparer           = new( ILEmitGenerator.EmitSortFunc<Target>(_sortBys) );
        private static readonly ComparerAdaptor<Target> _generatedComparer           = new( ExprTreeSortFuncCompiler.MakeSortFunc<Target>(_sortBys) );
        private static readonly ComparerAdaptor<Target> _handCoded                   = new( HandCoded );
        private static readonly ComparerAdaptor<Target> _intInlineHandCoded          = new( IntInline );       
        private static readonly ComparerAdaptor<Target> _composedFunctionsComparer   = new( ComposedFuncs );
        private static readonly ComparerAdaptor<Target> _combinatorFunctionsComparer = new( CombineFuncs(_composedSubFuncs) );

        private static readonly IComparer<Target> _nitoComparer =
            ComparerBuilder.For<Target>()
                .OrderBy(p => p.IntProp1)
                .ThenBy(p => p.StrProp1, StringComparer.Ordinal)
                .ThenBy(p => p.IntProp2)
                .ThenBy(p => p.StrProp2, StringComparer.Ordinal);

        private IOrderedEnumerable<Target> _lazyLinqOrderByThenBy;

        [GlobalSetup]
        public void Setup()
        {
            var dir = Path.Combine(Path.GetTempPath(), "targetData.data");
            var fs = new FileStream(dir, FileMode.Open, FileAccess.Read);
            _source = ProtoBuf.Serializer.Deserialize<List<Target>>(fs);
            _sortTargets = new Target[_source.Count];
            
            // lazy, evaluated in a benchmark and in the isValid function
            _lazyLinqOrderByThenBy = _source
                .OrderBy(x => x.IntProp1)
                .ThenBy(x => x.StrProp1, StringComparer.Ordinal)
                .ThenBy(x => x.IntProp2)
                .ThenBy(x => x.StrProp2, StringComparer.Ordinal);
        }
        
        [IterationSetup]
        public void ISetup()
        {
            // unsort _sortTargets. List.Sort is in-place, previous iterations will have sorted _sortTargets, resulting in sorting already sorted data
            for (int ctr = 0; ctr < _source.Count; ++ctr)
            {
                _sortTargets[ctr] = _source[ctr];
            }
        }        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpIntProp1(Target p1, Target p2) => p1.IntProp1.CompareTo(p2.IntProp1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpStrProp1(Target p1, Target p2) => CompareOrdinal(p1.StrProp1, p2.StrProp1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpIntProp2(Target p1, Target p2) => p1.IntProp2.CompareTo(p2.IntProp2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CmpStrProp2(Target p1, Target p2) => CompareOrdinal(p1.StrProp2, p2.StrProp2);

        private static Func<Target, Target, int> CombineFuncs(IEnumerable<Func<Target, Target, int>> funcs)
        {
            static Func<Target, Target, int> Combine(Func<Target, Target, int> fA, Func<Target, Target, int> fb) => (tA, tB) =>
                {
                    int tmp;
                    return (tmp = fA(tA, tB)) != 0 ? tmp : fb(tA, tB);
                };

            return funcs.Aggregate(Combine);
        }

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

        
        private static int IntInline(Target xx, Target yy)
        {
            int xxIp1 = xx.IntProp1;
            int aaIp1 = yy.IntProp1;
            if (xxIp1 < aaIp1) return -1;
            if (xxIp1 > aaIp1) return 1;
           
            int tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1);
            if(tmp != 0)
                return tmp;

            int xxIp2 = xx.IntProp2;
            int aaIp2 = yy.IntProp2;
            if (xxIp2 < aaIp2) return -1;
            return xxIp2 > aaIp2 ? 1 : CompareOrdinal(xx.StrProp2, yy.StrProp2);
        }        
        
        public bool IsValid()
        {
            Setup();
            ISetup();

            var referenceOrdering = _lazyLinqOrderByThenBy.ToList();

            var genSorted = _source.OrderBy(tt => tt, _generatedComparer).ToList();
            var composedFunctionsSorted = _source.OrderBy(m => m, _composedFunctionsComparer);
            var combinatorFunctionsSorted = _source.OrderBy(m => m, _combinatorFunctionsComparer);
            var handCodedTernarySorted = _source.OrderBy(m => m, _handCodedTernary).ToList();
            var inlineSorted = _source.OrderBy(m => m, _inlineComparer ).ToList();
            var intInlineSorted = _source.OrderBy(m => m, _intInlineComparer ).ToList();
            var genEmitSorted = _source.OrderBy(m => m, _ilEmittedComparer).ToList();

            return
                referenceOrdering.SequenceEqual(genSorted) &&
                referenceOrdering.SequenceEqual(composedFunctionsSorted) &&
                referenceOrdering.SequenceEqual(combinatorFunctionsSorted) &&
                referenceOrdering.SequenceEqual(handCodedTernarySorted) &&
                referenceOrdering.SequenceEqual(inlineSorted) &&
                referenceOrdering.SequenceEqual(intInlineSorted) &&
                referenceOrdering.SequenceEqual(genEmitSorted);
        }

        [Benchmark]
        public void ExprTreeGenerated() => Array.Sort(_sortTargets, _generatedComparer);

        [Benchmark]
        public void ILEmitted() => Array.Sort(_sortTargets, _ilEmittedComparer);

        [Benchmark]
        public void ComposedFunctions() => Array.Sort(_sortTargets, _composedFunctionsComparer);

        [Benchmark]
        public void CombinatorFunctions() => Array.Sort(_sortTargets, _combinatorFunctionsComparer);

        [Benchmark]
        public void HandCodedTernary() => Array.Sort(_sortTargets, _handCodedTernary);
        
        [Benchmark]
        public void IntInlineComparer() => Array.Sort(_sortTargets, _intInlineComparer);

        [Benchmark]
        public void InlineComparer() => Array.Sort(_sortTargets, _inlineComparer);        
        
        [Benchmark]
        public void HandCoded() => Array.Sort(_sortTargets, _handCoded);
        
        [Benchmark]
        public void IntInlineHandCoded() => Array.Sort(_sortTargets, _intInlineHandCoded);        

        [Benchmark]
        public void Nito() => Array.Sort(_sortTargets, _nitoComparer);
        
        [Benchmark]
        public void ExprTreeGeneratedOrderBy() => _sortTargets.OrderBy(m => m, _generatedComparer).Consume(_consumer);
        
        [Benchmark]
        public void LinqBaseLine() => _lazyLinqOrderByThenBy.Consume(_consumer);
    }
}