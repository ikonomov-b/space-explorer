// Documentation index generator and checker. Needs only the pinned .NET SDK (a file-based program).
//
//   dotnet run tools/docs.cs                 regenerate docs/index.md and the table in docs/decisions/README.md
//   dotnet run tools/docs.cs -- --check      exit 1 when a generated block is stale or a link, anchor, or record rule is broken
//   dotnet run tools/docs.cs -- --external   also verify that every cited external URL responds (network; not run in CI)
//
// Only Markdown files tracked by git are indexed, so a new document appears once it is committed.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

bool check = args.Contains("--check");
bool external = args.Contains("--external");
string root = RepositoryRoot();
List<Doc> docs = TrackedMarkdownFiles(root)
    .Select(path => Doc.Load(root, path))
    .OrderBy(d => d.RelativePath, StringComparer.Ordinal)
    .ToList();
Dictionary<string, Doc> byPath = docs.ToDictionary(d => d.RelativePath, StringComparer.Ordinal);
List<string> problems = new();

// 1. Internal links and anchors; collect back-references while resolving.
int linkCount = 0;
foreach (Doc doc in docs)
{
    string directory = Path.GetDirectoryName(doc.RelativePath) ?? "";
    foreach (Link link in doc.Links.Where(l => !l.IsExternal))
    {
        linkCount++;
        string target = link.Target.Length == 0 ? doc.RelativePath : Normalize(root, Path.Combine(directory, link.Target));
        if (byPath.TryGetValue(target, out Doc? targetDoc))
        {
            if (targetDoc != doc)
            {
                targetDoc.ReferencedFrom.Add(doc);
            }
            if (link.Anchor.Length > 0 && !targetDoc.Anchors.Contains(link.Anchor))
            {
                problems.Add($"{doc.RelativePath}: anchor not found: {link.Raw}");
            }
        }
        else if (!File.Exists(Path.Combine(root, target)) && !Directory.Exists(Path.Combine(root, target)))
        {
            problems.Add($"{doc.RelativePath}: link target not found: {link.Raw}");
        }
    }
}

// 2. Decision records: contiguous numbering, heading format, status, and at least one reference outside the indexes.
List<Doc> records = docs.Where(d => d.RecordNumber > 0).OrderBy(d => d.RecordNumber).ToList();
for (int i = 0; i < records.Count; i++)
{
    if (records[i].RecordNumber != i + 1)
    {
        problems.Add($"decision records are not contiguous: expected {i + 1:0000}, found {records[i].FileName}");
        break;
    }
}
foreach (Doc record in records)
{
    if (!record.Title.StartsWith($"{record.RecordNumber:0000}. ", StringComparison.Ordinal))
    {
        problems.Add($"{record.RelativePath}: heading must start with '{record.RecordNumber:0000}. '");
    }
    if (record.Status.Length == 0)
    {
        problems.Add($"{record.RelativePath}: header line has no 'Status: Proposed | Accepted | Superseded by NNNN'");
    }
    if (!record.ReferencedFrom.Any(d => !IsIndex(d)))
    {
        problems.Add($"{record.RelativePath}: not referenced from any document besides the indexes");
    }
}

// Partial supersessions are declared in the superseding record's header line, so earlier records stay unedited.
Dictionary<int, List<string>> supersessionNotes = new();
foreach (Doc record in records)
{
    foreach (Match m in Regex.Matches(record.HeaderLine, @"Supersedes\s+(.*?)\[decision (\d{4})\]"))
    {
        string scope = Regex.Replace(Regex.Replace(m.Groups[1].Value.Trim(), @"^the\s+", ""), @"\s+of$", "");
        string note = $"{(scope.Length == 0 ? "superseded" : scope + " superseded")} by [{record.RecordNumber:0000}]({record.FileName})";
        int target = int.Parse(m.Groups[2].Value);
        if (!supersessionNotes.TryGetValue(target, out List<string>? notes))
        {
            supersessionNotes[target] = notes = new();
        }
        notes.Add(note);
    }
}

// 3. Generated blocks.
StringBuilder decisionsTable = new();
decisionsTable.AppendLine("| # | Decision | Status |").AppendLine("| --- | --- | --- |");
foreach (Doc record in records)
{
    int split = record.Title.IndexOf(". ", StringComparison.Ordinal);
    string title = split >= 0 ? record.Title[(split + 2)..] : record.Title;
    string status = record.Status;
    if (supersessionNotes.TryGetValue(record.RecordNumber, out List<string>? notes) && !status.StartsWith("Superseded", StringComparison.Ordinal))
    {
        status += "; " + string.Join(", ", notes);
    }
    decisionsTable.AppendLine($"| [{record.RecordNumber:0000}]({record.FileName}) | {title} | {status} |");
}

StringBuilder index = new();
AppendIndexTable(index, "Documents", docs.Where(d => d.RelativePath.StartsWith("docs/", StringComparison.Ordinal) && d.RecordNumber == 0 && d.RelativePath != "docs/index.md" && !d.FileName.StartsWith("0000-", StringComparison.Ordinal)));
AppendIndexTable(index, "Decision records", records);
AppendIndexTable(index, "Repository guides", docs.Where(d => !d.RelativePath.StartsWith("docs/", StringComparison.Ordinal)));

UpdateBlock("docs/index.md", "docs-index", index.ToString());
UpdateBlock("docs/decisions/README.md", "decisions", decisionsTable.ToString());

// 4. External URLs, on request only.
if (external)
{
    HashSet<string> urls = docs.SelectMany(d => d.Links).Where(l => l.IsExternal).Select(l => l.Target).ToHashSet(StringComparer.Ordinal);
    using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SpaceExplorerDocsCheck/1.0");
    List<string> failures = new();
    await Parallel.ForEachAsync(urls, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (url, cancellation) =>
    {
        string failure = "";
        try
        {
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                failure = $"external URL returned {(int)response.StatusCode}: {url}";
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            failure = $"external URL failed: {url} ({e.Message})";
        }
        if (failure.Length > 0)
        {
            lock (failures)
            {
                failures.Add(failure);
            }
        }
    });
    Console.WriteLine($"{urls.Count} external URLs checked; {failures.Count} not OK.");
    problems.AddRange(failures.Order(StringComparer.Ordinal));
}

Console.WriteLine($"{docs.Count} documents, {linkCount} internal links, {records.Count} decision records; {problems.Count} problem(s).");
foreach (string problem in problems)
{
    Console.Error.WriteLine($"  {problem}");
}
return problems.Count == 0 ? 0 : 1;

void UpdateBlock(string relativePath, string name, string body)
{
    string path = Path.Combine(root, relativePath);
    string begin = $"<!-- generated:begin {name} -->";
    string end = $"<!-- generated:end {name} -->";
    string current = File.ReadAllText(path).Replace("\r\n", "\n");
    int i = current.IndexOf(begin, StringComparison.Ordinal);
    int j = current.IndexOf(end, StringComparison.Ordinal);
    if (i < 0 || j < i)
    {
        problems.Add($"{relativePath}: markers for generated block '{name}' not found");
        return;
    }
    string updated = current[..(i + begin.Length)] + "\n" + body.TrimEnd('\n') + "\n" + current[j..];
    if (updated == current)
    {
        return;
    }
    if (check)
    {
        problems.Add($"{relativePath}: generated block '{name}' is stale; run `dotnet run tools/docs.cs`");
        return;
    }
    File.WriteAllText(path, updated);
    Console.WriteLine($"updated {relativePath}");
}

void AppendIndexTable(StringBuilder builder, string heading, IEnumerable<Doc> rows)
{
    builder.AppendLine($"### {heading}").AppendLine();
    builder.AppendLine("| Document | Dated | Purpose | Referenced from |").AppendLine("| --- | --- | --- | --- |");
    foreach (Doc doc in rows)
    {
        string references = string.Join(", ", doc.ReferencedFrom
            .Where(r => !IsIndex(r))
            .OrderBy(Label, StringComparer.Ordinal)
            .Select(r => $"[{Label(r)}]({FromDocs(r)})"));
        builder.AppendLine($"| [{doc.Title}]({FromDocs(doc)}) | {doc.Date} | {doc.Purpose} | {(references.Length == 0 ? "none" : references)} |");
    }
    builder.AppendLine();
}

static bool IsIndex(Doc doc) => doc.RelativePath is "docs/index.md" or "docs/decisions/README.md";

static string Label(Doc doc)
{
    if (doc.RecordNumber > 0)
    {
        return doc.RecordNumber.ToString("0000");
    }
    string label = doc.RelativePath[..^3];
    return label.StartsWith("docs/", StringComparison.Ordinal) ? label[5..] : label;
}

static string FromDocs(Doc doc) => Path.GetRelativePath("docs", doc.RelativePath).Replace('\\', '/');

static string Normalize(string root, string relativePath) =>
    Path.GetRelativePath(root, Path.GetFullPath(Path.Combine(root, relativePath))).Replace('\\', '/');

static string RepositoryRoot()
{
    string? directory = Environment.CurrentDirectory;
    while (directory is not null && !Directory.Exists(Path.Combine(directory, ".git")) && !File.Exists(Path.Combine(directory, "SpaceExplorer.sln")))
    {
        directory = Path.GetDirectoryName(directory);
    }
    return directory ?? throw new InvalidOperationException("Run from inside the repository.");
}

static IEnumerable<string> TrackedMarkdownFiles(string root)
{
    ProcessStartInfo start = new("git", ["ls-files", "-z", "--", "*.md"]) { WorkingDirectory = root, RedirectStandardOutput = true };
    using Process process = Process.Start(start) ?? throw new InvalidOperationException("git is required to list tracked documents.");
    string output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException("git ls-files failed.");
    }
    return output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
}

sealed record Link(string Raw, string Target, string Anchor, bool IsExternal);

sealed class Doc
{
    private Doc(string relativePath, string title, int recordNumber, string headerLine, string status, string date, string purpose, HashSet<string> anchors, List<Link> links)
    {
        RelativePath = relativePath;
        Title = title;
        RecordNumber = recordNumber;
        HeaderLine = headerLine;
        Status = status;
        Date = date;
        Purpose = purpose;
        Anchors = anchors;
        Links = links;
    }

    public string RelativePath { get; }
    public string FileName => Path.GetFileName(RelativePath);
    public string Title { get; }
    public int RecordNumber { get; }
    public string HeaderLine { get; }
    public string Status { get; }
    public string Date { get; }
    public string Purpose { get; }
    public IReadOnlySet<string> Anchors { get; }
    public IReadOnlyList<Link> Links { get; }
    public HashSet<Doc> ReferencedFrom { get; } = new();

    public static Doc Load(string root, string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(root, relativePath)).Replace("\r\n", "\n");
        string[] lines = text.Split('\n');
        Match number = Regex.Match(Path.GetFileName(relativePath), @"^(\d{4})-");
        int recordNumber = relativePath.StartsWith("docs/decisions/", StringComparison.Ordinal) && number.Success ? int.Parse(number.Groups[1].Value) : 0;

        string title = "";
        string headerLine = "";
        HashSet<string> anchors = new(StringComparer.Ordinal);
        List<Link> links = new();
        bool inFence = false;
        foreach (string line in lines)
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }
            if (inFence)
            {
                continue;
            }
            Match heading = Regex.Match(line, @"^#{1,6}\s+(.*?)\s*$");
            if (heading.Success)
            {
                if (title.Length == 0 && line.StartsWith("# ", StringComparison.Ordinal))
                {
                    title = heading.Groups[1].Value;
                }
                anchors.Add(Slug(heading.Groups[1].Value));
            }
            if (headerLine.Length == 0 && line.StartsWith("Date: ", StringComparison.Ordinal))
            {
                headerLine = line;
            }
            foreach (Match m in Regex.Matches(line, @"\]\(([^)\s]+)\)"))
            {
                string raw = m.Groups[1].Value;
                bool isExternal = raw.StartsWith("http://", StringComparison.Ordinal) || raw.StartsWith("https://", StringComparison.Ordinal) || raw.StartsWith("mailto:", StringComparison.Ordinal);
                int hash = raw.IndexOf('#');
                links.Add(new Link(raw, isExternal ? raw : hash >= 0 ? raw[..hash] : raw, hash >= 0 && !isExternal ? raw[(hash + 1)..] : "", isExternal));
            }
        }

        Match status = Regex.Match(headerLine, @"Status:\s*(Proposed|Accepted|Superseded by \d{4})");
        Match date = Regex.Match(text, @"(?:Reviewed|Updated|Assessment date:|Date:)\s+(\d{4}-\d{2}-\d{2})");
        return new Doc(relativePath, title, recordNumber, headerLine, status.Success ? status.Groups[1].Value : "",
            date.Success ? date.Groups[1].Value : "undated", ExtractPurpose(lines, recordNumber > 0), anchors, links);
    }

    private static string ExtractPurpose(string[] lines, bool isRecord)
    {
        int start = isRecord
            ? Array.FindIndex(lines, l => l.StartsWith("## Context", StringComparison.Ordinal))
            : Array.FindIndex(lines, l => l.StartsWith("# ", StringComparison.Ordinal));
        List<string> paragraph = new();
        for (int i = start + 1; i < lines.Length && start >= 0; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0)
            {
                if (paragraph.Count > 0)
                {
                    break;
                }
                continue;
            }
            if (line.StartsWith('#') || line.StartsWith('|') || line.StartsWith("<!--", StringComparison.Ordinal) || line.StartsWith("```", StringComparison.Ordinal))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }
                continue;
            }
            paragraph.Add(line);
        }
        string prose = string.Join(" ", paragraph);
        prose = Regex.Replace(prose, @"\[([^\]]*)\]\([^)]*\)", "$1").Replace("**", "").Replace("`", "");
        prose = Regex.Replace(prose, @"^(?:Reviewed|Updated|Assessment date:|Date:)[^.]*\.\s*", "");
        prose = Regex.Replace(prose, @"^Status:[^.]*\.\s*", "");
        Match sentence = Regex.Match(prose, @"^(.+?\.)(?=\s+[A-Z0-9(]|$)");
        string purpose = sentence.Success ? sentence.Groups[1].Value : prose;
        if (purpose.Length > 220)
        {
            purpose = purpose[..217].TrimEnd() + "...";
        }
        return purpose.Replace("|", "\\|");
    }

    private static string Slug(string heading)
    {
        string text = Regex.Replace(heading, @"\[([^\]]*)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"[`*_]", "").Trim().ToLowerInvariant();
        text = Regex.Replace(text, @"[^\w\s-]", "");
        return Regex.Replace(text, @"\s+", "-");
    }
}
