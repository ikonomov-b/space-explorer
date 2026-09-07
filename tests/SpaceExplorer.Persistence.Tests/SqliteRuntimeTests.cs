using System.Text.RegularExpressions;
using Xunit;

namespace SpaceExplorer.Persistence.Tests;

public class SqliteRuntimeTests
{
    [Fact]
    public void Native_SQLite_library_loads_and_reports_a_version()
    {
        string version = SqliteRuntime.GetLibraryVersion();

        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+$"), version);
    }
}
