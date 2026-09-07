namespace SpaceExplorer.Core.Networking;

/// <summary>
/// Tracks which <see cref="CommandId"/>s have been applied and at which state revision, so a retried
/// command returns its original result instead of reprocessing ("commands carry unique IDs... retried
/// commands return their original results", technical design, artifact valuation and transactions).
/// Named apart from the save's HMAC-chained transaction ledger (decision 0018) so the two are never
/// confused; this journal knows nothing about what a result means, only the bytes recorded for it
/// (decision 0028).
/// </summary>
public sealed class CommandJournal
{
    private readonly Dictionary<CommandId, byte[]> _results = [];

    /// <summary>The revision the next accepted command must expect. Advances only on <see cref="RecordResult"/>.</summary>
    public ulong CurrentRevision { get; private set; }

    /// <summary>
    /// Whether <paramref name="id"/> may be accepted: it has no recorded result yet, and
    /// <paramref name="expectedRevision"/> matches <see cref="CurrentRevision"/>. A recorded ID is never
    /// acceptable again, even at the current revision — check <see cref="TryGetReplay"/> for it instead.
    /// </summary>
    public bool IsAcceptable(CommandId id, ulong expectedRevision) =>
        !_results.ContainsKey(id) && expectedRevision == CurrentRevision;

    /// <summary>Returns the result recorded for a previously applied <paramref name="id"/>, if any.</summary>
    public bool TryGetReplay(CommandId id, out byte[] result)
    {
        if (_results.TryGetValue(id, out byte[]? stored))
        {
            result = stored;
            return true;
        }

        result = [];
        return false;
    }

    /// <summary>Records <paramref name="result"/> for <paramref name="id"/> and advances <see cref="CurrentRevision"/> by one.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="id"/> already has a recorded result.</exception>
    public void RecordResult(CommandId id, byte[] result)
    {
        if (!_results.TryAdd(id, result))
        {
            throw new InvalidOperationException($"Command {id} already has a recorded result.");
        }

        CurrentRevision++;
    }
}
