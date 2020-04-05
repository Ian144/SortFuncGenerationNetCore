using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace SortFuncGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            TestDataCreation.CreateAndPersistData(50000);

            // checks all sort methods produce the same results
            var bmark = new Benchmarks();

            if (bmark.IsValid())
            {
                //IConfig cfg = DefaultConfig.Instance.With(Job.LegacyJitX64,Job.RyuJitX64,Job.VeryLongRun).With(ConfigOptions.DisableOptimizationsValidator);

                IConfig cfg = DefaultConfig.Instance.With(Job.RyuJitX64).With(ConfigOptions.DisableOptimizationsValidator);

                var summary = BenchmarkRunner.Run<Benchmarks>(cfg);
            }
            else
            {
                Console.WriteLine("invalid benchmark");
            }
        }
    }
}
