using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Nito.Comparers;
using static System.String;
// ReSharper disable InconsistentNaming


namespace SortFuncGeneration;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
//[SimpleJob(RuntimeMoniker.NetCoreApp31, baseline:true)]
//[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net70)]
public class Benchmarks {
    private static readonly List<SortDescriptor> _sortDescriptors = new()
    {
        new SortDescriptor(true, "IntProp1"),
        new SortDescriptor(true, "StrProp1"),
        new SortDescriptor(true, "IntProp2"),
        new SortDescriptor(true, "StrProp2"),
    };

    private static readonly Func<Target, Target, int>[] _composedSubFuncs = { CmpIntProp1, CmpStrProp1, CmpIntProp2, CmpStrProp2 };

    private static List<Target> _source;
    private static List<Target> _sortTargets;

    private static readonly Consumer _linqConsumer = new();

    private static readonly HandCodedComparer _handCodedComparer = new();
    private static readonly ComparerAdapter<Target> _ilEmittedComparer = new(ILEmitGenerator.EmitSortFunc<Target>(_sortDescriptors));
    private static readonly ComparerAdapter<Target> _exprTreeComparer = new(ExprTreeSortFuncCompiler.MakeSortFunc<Target>(_sortDescriptors));

    private static readonly ComparerAdapter<Target> _handCodedFuncComparer = new(HandCodedFunc);
    private static readonly ComparerAdapter<Target> _handCodedFuncLocalIntVarsComparer = new(HandCodedFuncLocalIntVars);
    private static readonly unsafe ComparerAdapterPtr _handCodedFuncPtrComparer = new(&HandCodedFunc);

    private static readonly ComparerAdapter<Target> _composedFunctionsComparer = new(ComposedFuncs);
    private static readonly ComparerAdapter<Target> _combinatorFunctionsComparer = new(CombineFuncs(_composedSubFuncs));

    private static readonly RoslynGenerator _rosGen = new();
    private static readonly IComparer<Target> _roslynComparer = _rosGen.GenComparer();

    private static readonly IComparer<Target> _nitoComparer =
        ComparerBuilder.For<Target>()
            .OrderBy(p => p.IntProp1)
            .ThenBy(p => p.StrProp1, StringComparer.Ordinal)
            .ThenBy(p => p.IntProp2)
            .ThenBy(p => p.StrProp2, StringComparer.Ordinal);

    private static IOrderedEnumerable<Target> _lazyLinqOrderByThenBy;

    [GlobalSetup]
    public static void GlobalSetup()
    {
        var dir = Path.Combine(Path.GetTempPath(), "targetData.data");
        var fs = new FileStream(dir, FileMode.Open, FileAccess.Read);
        _source = ProtoBuf.Serializer.Deserialize<List<Target>>(fs);
        _sortTargets = new List<Target>(_source.Count);

        foreach (var t in _source)
        {
            _sortTargets.Add(t);
        }

        // lazy, evaluated in a benchmark and in the isValid function
        _lazyLinqOrderByThenBy = _source
            .OrderBy(x => x.IntProp1)
            .ThenBy(x => x.StrProp1, StringComparer.Ordinal)
            .ThenBy(x => x.IntProp2)
            .ThenBy(x => x.StrProp2, StringComparer.Ordinal);
    }

    [IterationSetup]
    public void IterSetup()
    {
        // un-sort _sortTargets as _sortTargets.Sort is place, previous iterations will have sorted _sortTargets, resulting in sorting already sorted data
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
            int tmp = fA(tA, tB);
            return tmp != 0 ? tmp : fb(tA, tB);
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

    static int HandCodedFunc(Target xx, Target yy)
    {
        var xxIntProp1 = xx.IntProp1;
        var yyIntProp1 = yy.IntProp1;
        if (xxIntProp1 < yyIntProp1) return -1;
        if (xxIntProp1 > yyIntProp1) return 1;

        int tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1);
        if (tmp != 0)
            return tmp;

        var xxIntProp2 = xx.IntProp2;
        var yyIntProp2 = yy.IntProp2;
        if (xxIntProp2 < yyIntProp2) return -1;
        return xxIntProp2 > yyIntProp2
            ? 1
            : CompareOrdinal(xx.StrProp2, yy.StrProp2);
    }

    static int HandCodedFuncLocalIntVars(Target xx, Target yy)
    {
        var xxIntProp1 = xx.IntProp1;
        var yyIntProp1 = yy.IntProp1;
        if (xxIntProp1 < yyIntProp1) return -1;
        if (xxIntProp1 > yyIntProp1) return 1;

        int tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1);
        if (tmp != 0)
            return tmp;

        var xxIntProp2 = xx.IntProp2;
        var yyIntProp2 = yy.IntProp2;
        if (xxIntProp2 < yyIntProp2) return -1;
        if (xxIntProp2 > yyIntProp2) return -1;
        return CompareOrdinal(xx.StrProp2, yy.StrProp2);
    }


#pragma warning disable S125
    //static int HandCodedFunc(Target xx, Target yy)
    //{

    //    int tmp = xx.IntProp1.CompareTo(yy.IntProp1);
    //    if (tmp != 0)
    //        return tmp;

    //    tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1);
    //    if (tmp != 0)
    //        return tmp;

    //    tmp = xx.IntProp2.CompareTo(yy.IntProp2);
    //    if (tmp != 0)
    //        return tmp;

    //    return CompareOrdinal(xx.StrProp2, yy.StrProp2);
    //}
#pragma warning restore S125
        
    public bool IsValid()
    {
        GlobalSetup();
        IterSetup();

        var referenceOrdering = _lazyLinqOrderByThenBy.ToList();

        var exprTreeSorted = _source.OrderBy(tt => tt, _exprTreeComparer);
        var roslynSorted = _source.OrderBy(m => m, _roslynComparer);
        var ilEmitSorted = _source.OrderBy(m => m, _ilEmittedComparer);

        var composedFunctionsSorted = _source.OrderBy(m => m, _composedFunctionsComparer);
        var combinatorFunctionsSorted = _source.OrderBy(m => m, _combinatorFunctionsComparer);
        var handCodedSorted = _source.OrderBy(m => m, _handCodedComparer);
            
        return
            referenceOrdering.SequenceEqual(exprTreeSorted) &&
            referenceOrdering.SequenceEqual(composedFunctionsSorted) && 
            referenceOrdering.SequenceEqual(combinatorFunctionsSorted) &&
            referenceOrdering.SequenceEqual(roslynSorted) &&
            referenceOrdering.SequenceEqual(handCodedSorted) &&
            referenceOrdering.SequenceEqual(ilEmitSorted);
    }

    [Benchmark]
    public void RoslynGenerated() => _sortTargets.Sort(_roslynComparer);

    [Benchmark]
    public void ExprTreeGenerated() => _sortTargets.Sort(_exprTreeComparer);

    [Benchmark]
    public void ILEmitted() => _sortTargets.Sort(_ilEmittedComparer);

    [Benchmark]
    public void ComposedFunctions() => _sortTargets.Sort(_composedFunctionsComparer);

    [Benchmark]
    public void CombinatorFunctions() => _sortTargets.Sort(_combinatorFunctionsComparer);

    [Benchmark]
    public void HandCodedFunction() => _sortTargets.Sort(_handCodedFuncComparer);

    [Benchmark]
    public void HandCodedFunctionLocalIntVars() => _sortTargets.Sort(_handCodedFuncLocalIntVarsComparer);


    //[Benchmark]
    //public void HandCodedFunctionPtr() => _sortTargets.Sort(_handCodedFuncPtrComparer);

    //[Benchmark]
    //public void HandCodedComparer() => _sortTargets.Sort(_handCodedComparer);

    [Benchmark]
    public void Nito() => _sortTargets.Sort(_nitoComparer);

    //[Benchmark]
    //public void ExprTreeGeneratedOrderBy() => _sortTargets.OrderBy(m => m, _exprTreeComparer).Consume(_linqConsumer);

    [Benchmark]
    public void LinqBaseLine() => _lazyLinqOrderByThenBy.Consume(_linqConsumer);

    // commenting this out, as it will appear first in the ordered summary
    //[Benchmark]
    //public IComparer<Target> RoslynCompilation() =>  _rosGen.GenComparer();
}