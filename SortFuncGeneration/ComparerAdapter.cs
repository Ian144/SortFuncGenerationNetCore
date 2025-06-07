
using System;
using System.Collections.Generic;
using static System.String;

namespace SortFuncGeneration;

public class ComparerAdapter<T>(Func<T, T, int> sortFunc) : IComparer<T>
{
    public int Compare(T x, T y) => sortFunc(x, y);
}

internal sealed unsafe class ComparerAdapterPtr(delegate*<Target, Target, int> sortFunc) : IComparer<Target>
{
    public int Compare(Target x, Target y) => sortFunc(x, y);
}

internal sealed class HandCodedComparer : IComparer<Target>
{
    public int Compare(Target xx, Target yy)
    {
        if (xx.IntProp1 < yy.IntProp1) return -1;
        if (xx.IntProp1 > yy.IntProp1) return 1;

        int tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1);
        if (tmp != 0)
            return tmp;

        if (xx.IntProp2 < yy.IntProp2) return -1;
        return xx.IntProp2 > yy.IntProp2 
            ? 1 
            : CompareOrdinal(xx.StrProp2, yy.StrProp2);
    } 
}