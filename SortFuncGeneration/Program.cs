using System;
using System.Text;
using BenchmarkDotNet.Running;
using SortFuncGeneration;


TestDataCreation.CreateAndPersistData(200000);

var bmark = new Benchmarks();
if (bmark.IsValid())
{
    Console.WriteLine("valid benchmark");
    var _ = BenchmarkRunner.Run<Benchmarks>();
}
else
{
    Console.WriteLine("invalid benchmark");
}