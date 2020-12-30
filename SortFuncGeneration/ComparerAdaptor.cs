
using System;
using System.Collections.Generic;
using static System.String;

namespace SortFuncGeneration
{
    public class ComparerAdaptor<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _sortFunc;
        public ComparerAdaptor(Func<T, T, int> sortFunc) => _sortFunc = sortFunc;
        public int Compare(T x, T y) => _sortFunc(x, y);
    }

    public unsafe class ComparerAdaptorPtr : IComparer<Target>
    {
        private readonly delegate*<Target, Target, int> _sortFunc;
        public ComparerAdaptorPtr(delegate*<Target, Target, int> sortFunc) => _sortFunc = sortFunc;
        public int Compare(Target x, Target y) => _sortFunc(x, y);
    }

    public class HandcodedComparer : IComparer<Target>
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
}