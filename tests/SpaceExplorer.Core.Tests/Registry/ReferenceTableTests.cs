using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class ReferenceTableTests
{
    private static readonly PackId PackA = PackId.FromSpecificationHash(ContentHash.Of("pack-a"u8));
    private static readonly PackId PackB = PackId.FromSpecificationHash(ContentHash.Of("pack-b"u8));

    [Fact]
    public void A_handle_resolves_to_the_entry_at_its_index()
    {
        var first = new PrimitiveId(PackA, 1);
        var second = new PrimitiveId(PackB, 5);
        ReferenceTable table = ReferenceTable.Create([first, second]);

        Assert.Equal(2, table.Count);
        Assert.Equal(first, table.Resolve(new Handle(0)));
        Assert.Equal(second, table.Resolve(new Handle(1)));
    }

    [Fact]
    public void Resolving_a_handle_past_the_end_fails_explicitly()
    {
        ReferenceTable table = ReferenceTable.Create([new PrimitiveId(PackA, 1)]);
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
        var repeated = new PrimitiveId(PackA, 1);
        Assert.Throws<ArgumentException>(() => ReferenceTable.Create([repeated, repeated]));
    }

    [Fact]
    public void The_same_local_id_in_different_packs_is_not_a_repeat()
    {
        ReferenceTable table = ReferenceTable.Create([new PrimitiveId(PackA, 1), new PrimitiveId(PackB, 1)]);
        Assert.Equal(2, table.Count);
    }
}
