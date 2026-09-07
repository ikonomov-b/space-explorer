using Microsoft.Data.Sqlite;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>
/// <c>&lt;data root&gt;/index.db</c>: the index of verified immutable packages and records, with one manifest
/// per pack identifier enforced by a uniqueness constraint (decision 0039), and the reference index of
/// dependants maintained in the same publish-last transaction (decision 0040).
/// </summary>
internal sealed class PackageIndex : IDisposable
{
    private const int RecordKindManifest = 1;
    private const int RecordKindDefinition = 2;
    private const int DependantKindPackage = 1;
    private const int DependencyKindVocabulary = 2;

    private readonly SqliteConnection _connection;

    private PackageIndex(SqliteConnection connection) => _connection = connection;

    public static PackageIndex Open(DataRoot root)
    {
        Directory.CreateDirectory(root.Path);
        // Pooling off: a pooled connection keeps the file handle open after Dispose, which on Windows blocks
        // deleting or moving the data root. The index is opened briefly per operation, so pooling buys nothing.
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = root.IndexDatabasePath, Pooling = false }.ToString());
        connection.Open();
        var index = new PackageIndex(connection);
        index.Execute("PRAGMA foreign_keys = ON;");
        index.Execute("""
            CREATE TABLE IF NOT EXISTS packages (
                pack_id BLOB PRIMARY KEY CHECK (length(pack_id) = 16),
                manifest_hash BLOB NOT NULL UNIQUE CHECK (length(manifest_hash) = 32),
                registry_revision INTEGER NOT NULL CHECK (registry_revision >= 1),
                generator_version INTEGER NOT NULL CHECK (generator_version >= 1),
                grammar_version INTEGER NOT NULL CHECK (grammar_version >= 1),
                definition_count INTEGER NOT NULL CHECK (definition_count >= 0)
            );
            CREATE TABLE IF NOT EXISTS records (
                content_hash BLOB NOT NULL CHECK (length(content_hash) = 32),
                pack_id BLOB NOT NULL REFERENCES packages (pack_id),
                kind INTEGER NOT NULL,
                local_id INTEGER CHECK (local_id IS NULL OR local_id BETWEEN 1 AND 4294967295),
                PRIMARY KEY (pack_id, content_hash)
            );
            CREATE TABLE IF NOT EXISTS dependants (
                dependant_kind INTEGER NOT NULL,
                dependant_id BLOB NOT NULL,
                dependency_kind INTEGER NOT NULL,
                dependency_id BLOB NOT NULL,
                PRIMARY KEY (dependant_kind, dependant_id, dependency_kind, dependency_id)
            );
            """);
        return index;
    }

    /// <summary>The manifest hash indexed for <paramref name="pack"/>, or unset.</summary>
    public ContentHash FindManifest(PackId pack)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT manifest_hash FROM packages WHERE pack_id = $pack;";
        command.Parameters.AddWithValue("$pack", pack.Bytes.ToArray());
        return command.ExecuteScalar() is byte[] bytes ? ContentHash.FromBytes(bytes) : default;
    }

    /// <summary>Every indexed package with its manifest hash, in pack-identifier order.</summary>
    public IReadOnlyList<(PackId Pack, ContentHash Manifest, int DefinitionCount)> ListPackages()
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT pack_id, manifest_hash, definition_count FROM packages ORDER BY pack_id;";
        using SqliteDataReader reader = command.ExecuteReader();
        var packages = new List<(PackId, ContentHash, int)>();
        while (reader.Read())
        {
            packages.Add((PackId.FromBytes((byte[])reader[0]), ContentHash.FromBytes((byte[])reader[1]), reader.GetInt32(2)));
        }

        return packages;
    }

    /// <summary>Commits the package row, its record rows, and its reference-index edge in one transaction, last in the publish protocol.</summary>
    public void RegisterPackage(SetManifest manifest)
    {
        using SqliteTransaction transaction = _connection.BeginTransaction();
        byte[] pack = manifest.Pack.Bytes.ToArray();

        Execute(
            "INSERT INTO packages (pack_id, manifest_hash, registry_revision, generator_version, grammar_version, definition_count) VALUES ($pack, $manifest, $registry, $generator, $grammar, $count);",
            transaction,
            ("$pack", pack), ("$manifest", manifest.Hash.Bytes.ToArray()), ("$registry", (long)manifest.RegistryRevision), ("$generator", (long)manifest.GeneratorVersion), ("$grammar", (long)manifest.GrammarVersion), ("$count", (long)manifest.Definitions.Count));

        Execute(
            "INSERT INTO records (content_hash, pack_id, kind, local_id) VALUES ($hash, $pack, $kind, NULL);",
            transaction,
            ("$hash", manifest.Hash.Bytes.ToArray()), ("$pack", pack), ("$kind", (long)RecordKindManifest));

        foreach (ManifestEntry entry in manifest.Definitions)
        {
            Execute(
                "INSERT INTO records (content_hash, pack_id, kind, local_id) VALUES ($hash, $pack, $kind, $local);",
                transaction,
                ("$hash", entry.Hash.Bytes.ToArray()), ("$pack", pack), ("$kind", (long)RecordKindDefinition), ("$local", (long)entry.LocalId));
        }

        Execute(
            "INSERT INTO dependants (dependant_kind, dependant_id, dependency_kind, dependency_id) VALUES ($dkind, $did, $ykind, $yid);",
            transaction,
            ("$dkind", (long)DependantKindPackage), ("$did", pack), ("$ykind", (long)DependencyKindVocabulary), ("$yid", manifest.VocabularyHash.Bytes.ToArray()));

        transaction.Commit();
    }

    private void Execute(string sql, SqliteTransaction? transaction = null, params (string Name, object Value)[] parameters)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
