using BenchmarkDotNet.Running;

// Core measurements (development plan, verification targets). Run with:
//   dotnet run --project tests/SpaceExplorer.Benchmarks -c Release -- --filter '*'
// No benchmarks exist yet; the first ones arrive with the M0a generator.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
