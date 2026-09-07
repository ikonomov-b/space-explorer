# P0: greybox expedition prototype

Disposable. Nothing under `src/` references this folder and this folder references nothing under
`src/`. Built on the pinned Godot 4.7.2 .NET stack ([decision 0016](../../docs/decisions/0016-platform-confirmed-and-toolchain-pinned.md)),
as its own project so it builds and fails independently of `SpaceExplorer.sln`
([decision 0025](../../docs/decisions/0025-p0-assessment-location-scope-and-players.md)). Scope is exactly
[decision 0015](../../docs/decisions/0015-greybox-prototype.md): one hard-coded landing region, three sites,
four artifacts, a cargo limit forcing a choice of two, a return, a sale, and a second destination choice. No
generation, no persistence — closing the game loses everything. All state is composed in code, not the
editor, so there is no visual scene to open beyond `Main.tscn`'s empty root.

## Run it

```sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"
godot --path prototypes/p0-expedition            # play it directly
godot --path prototypes/p0-expedition --editor   # open the (near-empty) editor project
```

To hand a build to a player instead of running from source, export it (Linux shown; swap the preset
name and output extension for "Windows Desktop"):

```sh
godot --headless --path prototypes/p0-expedition --import
godot --headless --path prototypes/p0-expedition --export-release "Linux" "$(pwd)/build/p0/P0Expedition.x86_64"
build/p0/P0Expedition.x86_64
```

## Controls

| Input | Action |
| --- | --- |
| Mouse | Look |
| W A S D | Walk |
| E | Interact with whatever the bottom-of-screen prompt names |
| 1 / 2 | While cargo is full and a new find is offered: choose which carried artifact to leave behind |
| Esc | While cargo is full and a new find is offered: leave the new find where it is instead |
| Enter | At the sale screen: sell everything carried and move on |
| 1 / 2 / 3 / 4 | At the destination screen: pick a next destination, or end the session |

## What's in the region

Three sites, four artifacts, reachable on foot from the yellow home anchor pad at the start point:

| Site | Artifact | Sale price |
| --- | --- | --- |
| Ridge Site | Fused Regolith Core | 90c |
| Crater Site | Banded Ice Shard | 140c |
| Crater Site | Pitted Alloy Fragment | 60c |
| Wreck Site | Sealed Data Spindle | 220c |

Cargo holds two. Whichever two artifacts a player is not carrying when they interact with the home
anchor are never sold; the swap prompt at the moment a third find is offered is where the trade-off
becomes a decision instead of an accident.

## Running a session (decision 0015's exit record)

Three to five players, one at a time, no coaching beyond the controls table above. Watch, don't
prompt. For each player record:

- Can they explain, in their own words, a choice (which two artifacts, or which destination) that
  changed their outcome?
- At the destination screen, do they pick a next destination, or end the session?
- Anything they got stuck on, misread, or asked about unprompted.

| Player | Can explain a changed-outcome choice? | Chose another trip? | Notes |
| --- | --- | --- | --- |
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |

Findings feed M1's design per decision 0015. Recruiting and running these sessions is the owner's task
(see [decision 0025](../../docs/decisions/0025-p0-assessment-location-scope-and-players.md)); nothing
in this repository can substitute for a real player's answer to the two questions above. Once sessions
are run, fill in the table and add a short summary here, then update [progress.md](../../docs/progress.md#milestones).
