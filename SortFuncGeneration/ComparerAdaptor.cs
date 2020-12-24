
using System;
using System.Collections.Generic;
using static System.String;

namespace SortFuncGeneration
{
    // making ComparerAdaptor a readonly struct slowed down sorting, and increased allocs from 136 to 160 bytes
    public class ComparerAdaptor<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _sortFunc;
        public ComparerAdaptor(Func<T, T, int> sortFunc) => _sortFunc = sortFunc;
        public int Compare(T x, T y) => _sortFunc(x, y);
    }
    
    
    // making ComparerAdaptor a readonly struct slowed down sorting, and increased allocs from 136 to 160 bytes
    public class InlineComparerAdaptor : IComparer<Target>
    {
        public int Compare(Target xx, Target yy)
        {
            int tmp;
            return (tmp = xx.IntProp1.CompareTo(yy.IntProp1)) != 0
                ? tmp
                : (tmp = CompareOrdinal(xx.StrProp1, yy.StrProp1)) != 0
                    ? tmp
                    : (tmp = xx.IntProp2.CompareTo(yy.IntProp2)) != 0
                        ? tmp
                        : CompareOrdinal(xx.StrProp2, yy.StrProp2);
        } 
    }    
    
    
    public class InlineComparerAdaptor2 : IComparer<Target>
    {
        public int Compare(Target xx, Target yy)
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
            if (xxIp2 > aaIp2) return 1;
            
            return CompareOrdinal(xx.StrProp2, yy.StrProp2);
        } 
    }     
    
}