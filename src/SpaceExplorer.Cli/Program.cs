using System.Runtime.InteropServices;
using SpaceExplorer.Persistence;

// Primitive-set generator, validator, and inspector (M0a). No generation command exists yet;
// `diagnostics` proves that the packaged CLI runs and loads the native SQLite library on each platform.
return args switch
{
    [] or ["--help" or "-h"] => Usage(),
    ["diagnostics"] => Diagnostics(),
    _ => Unknown(args[0]),
};

static int Usage()
{
    Console.WriteLine("Usage: SpaceExplorer.Cli <command>");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  diagnostics   Print runtime, platform, and SQLite library versions.");
    Console.WriteLine();
    Console.WriteLine("Planned for M0a: generate-set, validate, inspect.");
    return 0;
}

static int Diagnostics()
{
    Console.WriteLine($"runtime  {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"platform {RuntimeInformation.RuntimeIdentifier} ({RuntimeInformation.OSDescription})");
    Console.WriteLine($"sqlite   {SqliteRuntime.GetLibraryVersion()}");
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run with --help for usage.");
    return 2;
}
