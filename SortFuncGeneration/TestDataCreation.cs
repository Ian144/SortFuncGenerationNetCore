using System;
using System.IO;
using System.Linq;
using FsCheck;
using SortFuncCommon;
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

namespace SortFuncGeneration
{
    class ArbitrarySimpleString : Arbitrary<string>
    {
        //public override Gen<DateTime> Generator => Gen.Choose(-999, 999).Select(i => DateTime.UtcNow.AddMinutes(i));
        public override Gen<string> Generator
        {
            get
            {
                var myCharGen = Gen.Choose(97, 122).Select(Convert.ToChar);
                var myCharArrayGen = Gen.ArrayOf(myCharGen).Where(xs => xs.Any());
                var myStringGen = myCharArrayGen.Select(arr => new string(arr));
                return myStringGen;
            }
        }
    }

    class MyArbitraries
    {
        // ReSharper disable once UnusedMember.Global
        public static Arbitrary<string> String() => new ArbitrarySimpleString();
    }


    public static class TestDataCreation
    {
        public static void CreateAndPersistData(int size)
        {
            Arb.Register<MyArbitraries>();

            var targets = Arb.From<Target>().Generator.Sample(size);

            using (var fs = new FileStream("c:\\temp\\targetData.data", FileMode.Create))
            {
                ProtoBuf.Serializer.Serialize(fs, targets);
            }
        }
    }
}
