using SpaceExplorer.Core.Networking;
using Xunit;

namespace SpaceExplorer.Core.Tests.Networking;

public class CommandJournalTests
{
    [Fact]
    public void A_fresh_id_at_the_current_revision_is_acceptable()
    {
        var journal = new CommandJournal();
        Assert.True(journal.IsAcceptable(new CommandId(1), expectedRevision: 0));
    }

    [Fact]
    public void Recording_a_result_advances_the_revision_by_exactly_one()
    {
        var journal = new CommandJournal();
        journal.RecordResult(new CommandId(1), [9]);

        Assert.Equal(1UL, journal.CurrentRevision);
    }

    [Fact]
    public void A_stale_revision_is_rejected_but_is_not_a_replay()
    {
        var journal = new CommandJournal();
        journal.RecordResult(new CommandId(1), [9]); // current revision is now 1

        var staleId = new CommandId(2);
        Assert.False(journal.IsAcceptable(staleId, expectedRevision: 0));
        Assert.False(journal.TryGetReplay(staleId, out _));
    }

    [Fact]
    public void Resubmitting_a_recorded_id_replays_its_original_result_and_is_never_acceptable_again()
    {
        var journal = new CommandJournal();
        var id = new CommandId(1);
        byte[] original = [1, 2, 3];
        journal.RecordResult(id, original);
        ulong revisionAfterFirstRecord = journal.CurrentRevision;

        Assert.True(journal.TryGetReplay(id, out byte[] replay));
        Assert.Equal(original, replay);

        // Not acceptable even resubmitted at the (new) current revision: a replay is not a fresh command.
        Assert.False(journal.IsAcceptable(id, expectedRevision: journal.CurrentRevision));
        Assert.Equal(revisionAfterFirstRecord, journal.CurrentRevision);
    }

    [Fact]
    public void Recording_the_same_id_twice_fails_explicitly()
    {
        var journal = new CommandJournal();
        var id = new CommandId(1);
        journal.RecordResult(id, [1]);

        Assert.Throws<InvalidOperationException>(() => journal.RecordResult(id, [2]));
    }

    [Fact]
    public void An_unrecorded_id_has_no_replay()
    {
        var journal = new CommandJournal();
        Assert.False(journal.TryGetReplay(new CommandId(1), out byte[] result));
        Assert.Empty(result);
    }
}
