using ProtoBuf;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global used implicitly by fscheck
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SortFuncCommon
{
    [ProtoContract]
    public class Target
    {
        [ProtoMember(1)]
        public int IntProp1 { get; set; }

        [ProtoMember(2)]
        public int IntProp2 { get; set; }

        [ProtoMember(3)]
        public string StrProp1 { get; set; }

        [ProtoMember(4)]
        public string StrProp2 { get; set; }

        public override string ToString() => $"{IntProp1} : {IntProp2} : {StrProp1} : {StrProp2}";
    }
}