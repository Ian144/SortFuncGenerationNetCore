

namespace SortFuncGeneration
{
    public record SortDescriptor(bool Ascending, string PropName);
    public record Target(int IntProp1, int IntProp2, string StrProp1, string StrProp2);
}    