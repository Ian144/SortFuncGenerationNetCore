using System;
using System.Collections.Generic;

namespace SortFuncGeneration
{
    // making ComparerAdaptor a readonly struct slowed down sorting, and increased allocs from 136 to 160 bytes
    public class ComparerAdaptor<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _sortFunc;
        public ComparerAdaptor(Func<T, T, int> sortFunc) => _sortFunc = sortFunc;
        public int Compare(T x, T y) => _sortFunc(x, y);
    }
}