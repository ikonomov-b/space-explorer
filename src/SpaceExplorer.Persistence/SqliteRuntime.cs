using Microsoft.Data.Sqlite;

namespace SpaceExplorer.Persistence;

/// <summary>
/// Diagnostics for the SQLite adapter. Loading the native library is the platform-specific step
/// that every packaged build must prove on Linux and Windows (technology decision, packaging policy).
/// </summary>
public static class SqliteRuntime
{
    /// <summary>Opens an in-memory database and returns the version reported by the native SQLite library.</summary>
    public static string GetLibraryVersion()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        return (string)command.ExecuteScalar()!;
    }
}
