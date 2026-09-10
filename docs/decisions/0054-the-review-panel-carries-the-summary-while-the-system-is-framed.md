# 0054. The review panel carries the summary while the system is framed and the whole table at a body, and a key hides it

Date: 2026-09-10. Status: Accepted. Source: the owner's instruction of 2026-09-10 to take the recommended path. Clears [review finding 49](../review.md#49-the-review-views-panel-and-the-system-it-describes-compete-for-one-screen). Supersedes the whole-text panel clause of [decision 0052](0052-the-two-levers-compose-a-destination-and-a-review-view-shows-it.md); its caption, scale, movement, and harness clauses stand.

## Context

[Decision 0052](0052-the-two-levers-compose-a-destination-and-a-review-view-shows-it.md) puts the bodies of a composed system beside "the whole text beside it". Rendering the twenty-four starter destinations of iteration 3's reading showed that the two cannot both have the screen: on an eight-planet system the table is 1,253 by 517 pixels of a 1600 by 900 window, 45 per cent of it, and the system is drawn behind it, so seven of that system's sixteen bodies fell inside the panel's rectangle. A caption can be placed clear of the panel, and is; a body cannot, because it is where the system puts it. The view's interim compromise was a panel ground at 55 per cent opacity, which left both readable and neither clean, and that is [finding 49](../review.md#49-the-review-views-panel-and-the-system-it-describes-compete-for-one-screen).

## Decision

**The panel's content follows where the view is.** While the whole system is framed, the panel carries the summary alone: what was asked for, the attempt it took, the description version, the graph, the pins, the star, the totals, the verdict, and the legend — nine rows against an eight-planet system's twenty-four — because every body already wears its own row of the table. At a body, the whole table appears, where one row is what matters and the system is out of frame anyway. Moving the view between the two changes the panel's form and nothing else; the text is the same text, derived once.

**The panel and the captions divide the work by distance.** While the table is shown, a body too far from the camera to be read closely wears nothing, since the table lists it. While the summary is shown, every body wears its short line and a near one its whole line, which is decision 0052's rule unchanged and now the only place a body's figures appear in that view.

**A key hides the panel.** `p` hides and shows it at the window, and `--no-panel` opens without it, so a picture of the system alone is available from a terminal, as `--screenshot` makes a picture available without one. The panel's ground is opaque, because with the summary in the framed view nothing worth seeing is behind it.

## Consequences

Easier: the framed view shows the whole system — all sixteen bodies of the sample's busiest system, where the previous panel dimmed seven of them behind its text; a reviewer reads a table where a table serves and a picture where a picture does; and a record can hold a clean picture of a system beside the text it is judged by rather than one picture trying to be both.

Harder: what a picture contains now depends on where the view is as well as on the two levers, so a sample states which form it was taken in, framed or at a body. The summary rows are told from the body rows by their indentation in the rendered description, which couples the harness to that rendering; a second rendering in the core would decouple them and is the kind of scaffolding [decision 0024](0024-tests-comments-index-and-scaffolding-trimmed.md) says to wait for a need for.

Verified by the pictures of all three forms in the reading sample of [progress](../progress.md#solar-system-iterations), and by the exported-build smoke check, which the review path must not disturb.

## Applied to

- `src/SpaceExplorer.Game/SystemView.cs`, `src/SpaceExplorer.Game/Main.cs`
- [Progress: M0a exit criteria, solar-system iterations](../progress.md#solar-system-iterations)
- [Review finding 49](../review.md#49-the-review-views-panel-and-the-system-it-describes-compete-for-one-screen), [docs/tools.md](../tools.md#godot)
