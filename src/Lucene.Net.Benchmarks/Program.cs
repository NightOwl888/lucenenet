using BenchmarkDotNet.Running;
using System;

namespace Lucene.Net.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
            BenchmarkRunner.Run<BenchmarkDictionary>();

            //#if DEBUG
            Console.ReadKey();
            //#endif
        }
    }
}
