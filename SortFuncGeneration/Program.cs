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
            //TestDataCreation.CreateAndPersistData(100000);

            var bmark = new Benchmarks();
            if (bmark.IsValid())
            {
                //IConfig cfg = DefaultConfig.Instance.With( Job.RyuJitX64, Job.VeryLongRun).With(ConfigOptions.DisableOptimizationsValidator);
                //IConfig cfg = DefaultConfig.Instance.AddJob(Job.RyuJitX64);
                IConfig cfg = DefaultConfig.Instance.AddJob(Job.ShortRun);

                var _ = BenchmarkRunner.Run<Benchmarks>(cfg);
            }
            else
            {
                Console.WriteLine("invalid benchmark");
            }
        }
    }
}
