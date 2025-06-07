using System;
using BenchmarkDotNet.Running;
using SortFuncGeneration;


TestDataCreation.CreateAndPersistData(200000);

var benchmarks = new Benchmarks();
if (benchmarks.IsValid())
{
    Console.WriteLine("valid benchmark");
    var _ = BenchmarkRunner.Run<Benchmarks>();
}
else
{
    Console.WriteLine("invalid benchmark");
}