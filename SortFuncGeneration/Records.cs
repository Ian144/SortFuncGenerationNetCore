

using ProtoBuf;

namespace SortFuncGeneration;

[ProtoContract(SkipConstructor = true, ImplicitFields = ImplicitFields.AllFields)]
public record SortDescriptor(bool Ascending, string PropName);

[ProtoContract(SkipConstructor = true, ImplicitFields = ImplicitFields.AllFields)]
public record Target(int IntProp1, int IntProp2, string StrProp1, string StrProp2);