using System;
using System.IO;
using System.Linq;
using FsCheck;
// ReSharper disable ConvertToStaticClass

#nullable enable

// ReSharper disable ClassNeverInstantiated.Global

namespace SortFuncGeneration;

sealed class ArbitrarySimpleString : Arbitrary<string>
{
    public override Gen<string> Generator => Gen
        .ArrayOf(Arb.Generate<char>())
        .Where(xs => xs.Length != 0)
        .Select(arr => new string(arr));
}

sealed class ArbitrarySimpleChar : Arbitrary<char>
{
    public override Gen<char> Generator => 
        from ii in Gen.Choose(97, 122)
        select Convert.ToChar(ii);
}

sealed class ArbitraryTarget : Arbitrary<Target>
{
    public override Gen<Target> Generator =>
        from i1 in Arb.Generate<int>()
        from i2 in Arb.Generate<int>()
        from s1 in Arb.Generate<string>()
        from s2 in Arb.Generate<string>()
        select new Target(i1, i2, s1, s2);
}

sealed class MyArbitraries
{
    private MyArbitraries() {}
    public static Arbitrary<Target> Target() => new ArbitraryTarget();
    public static Arbitrary<string> String() => new ArbitrarySimpleString();
    public static Arbitrary<char> Char() => new ArbitrarySimpleChar();
}

public static class TestDataCreation
{
    public static void CreateAndPersistData(int size)
    {
        Arb.Register<MyArbitraries>();
        var targets = Arb.From<Target>().Generator.Sample(size);
        var dir = Path.Combine(Path.GetTempPath(), "targetData.data");
        using var fs = new FileStream(dir, FileMode.Create);
        ProtoBuf.Serializer.Serialize(fs, targets);
    }
}