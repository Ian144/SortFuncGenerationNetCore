using System;
using BenchmarkDotNet.Running;
using SortFuncGeneration;


TestDataCreation.CreateAndPersistData(100000);

var bmark = new Benchmarks();
if (bmark.IsValid())
{
    var _ = BenchmarkRunner.Run<Benchmarks>();
}
else
{
    Console.WriteLine("invalid benchmark");
}