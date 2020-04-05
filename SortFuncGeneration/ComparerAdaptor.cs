using System;
using System.Collections.Generic;

namespace SortFuncGeneration
{
    public class ComparerAdaptor<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _sortFunc;
        public ComparerAdaptor(Func<T, T, int> sortFunc) => _sortFunc = sortFunc;
        public int Compare(T x, T y) => _sortFunc(x, y);
    }
}