using Microsoft.Data.Sqlite;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>One indexed composition graph: what a loader needs before it can read the record itself.</summary>
internal sealed record GraphRow(PackId Pack, ContentHash GraphHash, PackId SourcePack, uint RegistryRevision, uint GrammarVersion, int NodeCount, CompositionDomain Domain);

/// <summary>
/// <c>&lt;data root&gt;/index.db</c>: the index of verified immutable packages, records, and composition
/// graphs, with one manifest per pack identifier and one graph per graph pack identifier enforced by
/// uniqueness constraints (decision 0039), and the reference index of dependants maintained in the same
/// publish-last transaction (decision 0040).
/// </summary>
internal sealed class PackageIndex : IDisposable
{
    private const int RecordKindManifest = 1;
    private const int RecordKindDefinition = 2;
    private const int DependantKindPackage = 1;
    private const int DependantKindGraph = 2;
    private const int DependencyKindPack = 1;
    private const int DependencyKindVocabulary = 2;
    private const int DependencyKindGrammar = 3;

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
            CREATE TABLE IF NOT EXISTS graphs (
                pack_id BLOB PRIMARY KEY CHECK (length(pack_id) = 16),
                graph_hash BLOB NOT NULL UNIQUE CHECK (length(graph_hash) = 32),
                source_pack_id BLOB NOT NULL REFERENCES packages (pack_id),
                registry_revision INTEGER NOT NULL CHECK (registry_revision >= 1),
                generator_version INTEGER NOT NULL CHECK (generator_version >= 1),
                grammar_version INTEGER NOT NULL CHECK (grammar_version >= 1),
                domain INTEGER NOT NULL CHECK (domain >= 1),
                node_count INTEGER NOT NULL CHECK (node_count >= 1)
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

    /// <summary>The graph indexed under <paramref name="pack"/>, or null.</summary>
    public GraphRow? FindGraph(PackId pack)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT graph_hash, source_pack_id, registry_revision, grammar_version, node_count, domain FROM graphs WHERE pack_id = $pack;";
        command.Parameters.AddWithValue("$pack", pack.Bytes.ToArray());
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? Row(pack, reader) : null;
    }

    /// <summary>Every indexed graph, in pack-identifier order.</summary>
    public IReadOnlyList<GraphRow> ListGraphs()
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT graph_hash, source_pack_id, registry_revision, grammar_version, node_count, domain, pack_id FROM graphs ORDER BY pack_id;";
        using SqliteDataReader reader = command.ExecuteReader();
        var graphs = new List<GraphRow>();
        while (reader.Read())
        {
            graphs.Add(Row(PackId.FromBytes((byte[])reader[6]), reader));
        }

        return graphs;
    }

    /// <summary>Commits the graph row and its reference-index edges in one transaction, last in the publish protocol.</summary>
    public void RegisterGraph(CompositionGraph graph)
    {
        GraphSpecification specification = graph.Specification;
        using SqliteTransaction transaction = _connection.BeginTransaction();
        byte[] pack = graph.Pack.Bytes.ToArray();

        Execute(
            "INSERT INTO graphs (pack_id, graph_hash, source_pack_id, registry_revision, generator_version, grammar_version, domain, node_count) VALUES ($pack, $graph, $source, $registry, $generator, $grammar, $domain, $count);",
            transaction,
            ("$pack", pack), ("$graph", graph.Hash.Bytes.ToArray()), ("$source", specification.SourcePack.Bytes.ToArray()), ("$registry", (long)specification.RegistryRevision), ("$generator", (long)specification.GeneratorVersion), ("$grammar", (long)specification.GrammarVersion), ("$domain", (long)specification.CompositionDomain), ("$count", (long)graph.NodeCount));

        Execute(
            "INSERT INTO dependants (dependant_kind, dependant_id, dependency_kind, dependency_id) VALUES ($dkind, $did, $ykind, $yid);",
            transaction,
            ("$dkind", (long)DependantKindGraph), ("$did", pack), ("$ykind", (long)DependencyKindPack), ("$yid", specification.SourcePack.Bytes.ToArray()));

        Execute(
            "INSERT INTO dependants (dependant_kind, dependant_id, dependency_kind, dependency_id) VALUES ($dkind, $did, $ykind, $yid);",
            transaction,
            ("$dkind", (long)DependantKindGraph), ("$did", pack), ("$ykind", (long)DependencyKindGrammar), ("$yid", specification.GrammarHash.Bytes.ToArray()));

        transaction.Commit();
    }

    private static GraphRow Row(PackId pack, SqliteDataReader reader) => new(
        pack,
        ContentHash.FromBytes((byte[])reader[0]),
        PackId.FromBytes((byte[])reader[1]),
        (uint)reader.GetInt64(2),
        (uint)reader.GetInt64(3),
        reader.GetInt32(4),
        (CompositionDomain)reader.GetInt64(5));

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
