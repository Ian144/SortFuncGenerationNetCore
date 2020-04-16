using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace SortFuncGeneration
{
    class Program
    {
        static void Main()
        {
            TestDataCreation.CreateAndPersistData(50000);

            var bmark = new Benchmarks();
            if (bmark.IsValid())
            {
                //IConfig cfg = DefaultConfig.Instance.With( Job.RyuJitX64, Job.VeryLongRun).With(ConfigOptions.DisableOptimizationsValidator);
                IConfig cfg = DefaultConfig.Instance.With( Job.RyuJitX64).With(ConfigOptions.Default);
                var _ = BenchmarkRunner.Run<Benchmarks>(cfg);
            }
            else
            {
                Console.WriteLine("invalid benchmark");
            }
        }
    }
}
