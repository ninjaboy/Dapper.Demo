using System;

namespace Dapper.Demo.Benchmarks
{
    using System;
    using BenchmarkDotNet.Running;

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<TestSuite>();
            Console.ReadLine();
        }
    }
}
