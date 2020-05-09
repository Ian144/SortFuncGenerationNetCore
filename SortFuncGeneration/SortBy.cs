namespace SortFuncGeneration
{
    public readonly struct SortBy
    {
        public SortBy(bool ascending, string propName)
        {
            Ascending = ascending;
            PropName = propName;
        }

        public bool Ascending { get; }
        public string PropName { get; }
    }
}