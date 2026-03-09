using BenchmarkDotNet.Running;

namespace Picea.Benchmarks;

public class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
