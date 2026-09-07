using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class ReferenceTableTests
{
    private static readonly PackId PackA = PackId.FromSpecificationHash(ContentHash.Of("pack-a"u8));
    private static readonly PackId PackB = PackId.FromSpecificationHash(ContentHash.Of("pack-b"u8));

    private static PrimitiveRevisionRef Ref(PackId pack, uint localId, string content) =>
        new(new PrimitiveId(pack, localId), ContentHash.Of(System.Text.Encoding.ASCII.GetBytes(content)));

    [Fact]
    public void A_handle_resolves_to_the_exact_reference_at_its_index()
    {
        PrimitiveRevisionRef first = Ref(PackA, 1, "a1");
        PrimitiveRevisionRef second = Ref(PackB, 5, "b5");
        ReferenceTable table = ReferenceTable.Create([first, second]);

        Assert.Equal(2, table.Count);
        Assert.Equal(first, table.Resolve(new Handle(0)));
        Assert.Equal(second, table.Resolve(new Handle(1)));
    }

    [Fact]
    public void Resolving_a_handle_past_the_end_fails_explicitly()
    {
        ReferenceTable table = ReferenceTable.Create([Ref(PackA, 1, "a1")]);
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Resolve(new Handle(1)));
    }

    [Fact]
    public void An_empty_table_rejects_every_handle()
    {
        ReferenceTable table = ReferenceTable.Create([]);
        Assert.Equal(0, table.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Resolve(new Handle(0)));
    }

    [Fact]
    public void Building_a_table_rejects_an_unset_entry()
    {
        Assert.Throws<ArgumentException>(() => ReferenceTable.Create([default]));
    }

    [Fact]
    public void Building_a_table_rejects_a_repeated_identity()
    {
        PrimitiveRevisionRef repeated = Ref(PackA, 1, "a1");
        Assert.Throws<ArgumentException>(() => ReferenceTable.Create([repeated, repeated]));
    }

    [Fact]
    public void Two_revisions_of_one_identity_are_also_rejected()
    {
        // A generated ID has one hash for life (decision 0033), so a table naming two is malformed.
        Assert.Throws<ArgumentException>(() => ReferenceTable.Create([Ref(PackA, 1, "a1"), Ref(PackA, 1, "a1-other")]));
    }

    [Fact]
    public void The_same_local_id_in_different_packs_is_not_a_repeat()
    {
        ReferenceTable table = ReferenceTable.Create([Ref(PackA, 1, "a1"), Ref(PackB, 1, "b1")]);
        Assert.Equal(2, table.Count);
    }
}
