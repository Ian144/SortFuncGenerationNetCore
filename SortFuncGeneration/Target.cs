using System.Diagnostics.CodeAnalysis;
using ProtoBuf;

//#nullable enable
// ReSharper disable UnusedAutoPropertyAccessor.Global used implicitly by fscheck
// ReSharper disable ClassNeverInstantiated.Global

namespace SortFuncGeneration
{
    // shortform won't work with protobuf.net, which requires a default ctor
    //[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    //public record Target(int IntProp1, int IntProp2, string StrProp1, string StrProp2);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public record Target
    {
        public int IntProp1 { get; init; }
        public int IntProp2 { get; init; }
        public string StrProp1 { get; init; }
        public string StrProp2 { get; init; }
    }
}