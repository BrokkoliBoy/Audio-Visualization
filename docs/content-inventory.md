# Content Inventory - Songs & Views

**This file is meant to be kept up to date.** Whenever a song is added/removed/rewired, or a view's
status changes, update the relevant row here in the same session.

Data below was pulled directly from the live Editor (2026-09-23): every `AudioClipContainer` in the
open scene via `FindObjectsByType`, and `ViewManager`'s configured `_views` list via reflection
(read in Edit mode, since the singletons only populate on Play).

## Songs

All 8 are wired into the scene under `Game/Audio/Audio Clips/`, cycled independently of the current
view via F1 (next) / F2 (previous) on `SimpleControl`. A 9th slot is a live microphone input
instead of a fixed clip.

| Song | Length | BPM | Licensing note |
|---|---|---|---|
| Pure Gold 3 - Niklas Ahlstrom | 2:24 | 128 | Commercially released track |
| Peeping Tom - Jamie Berry ft. Rosie Harte | 3:30 | 100 | Commercially released track |
| 20 - 20,000 Hz Audio | 0:36 | - | Test tone sweep, not a song, likely fine to keep |
| Stranger Things Theme Song (C418 Remix) | 4:08 | 84 | Commercial IP (Netflix show) + a named remixer's derivative work |
| GRiZ - A Fine Way To Die feat. Orlando Napier | 6:42 | 103 | Commercially released track |
| Pizza Rolls - Shawn Wasabi | 4:11 | 110 | Commercially released track |
| JUJUTSU KAISEN - Ending Lost in Paradise feat. AKLO | 1:29 | 122 | Licensed anime ending theme |
| Thomas Vent - One Day Live Your Life | 3:50 | 110 | Commercially released track |
| *(Microphone input)* | live | - | Not a file, no licensing concern |

**Also present in `Assets/Assets/Audio/Clips/` but not wired to any `AudioClipContainer` in the
current scene** (unused, still committed to git, still a licensing/repo-size concern for no
current benefit):

- `Bravo_Traube_Werbung_ohne_rauschen.mp3` (a radio ad jingle)
- `Song of Storms - The Legend of Zelda Ocarina of Time [Piano Tutorial] (Synthesia) Anifuse.mp3` (Nintendo IP)
- `Zelda - Piano (Piano Man).mp3` (Nintendo IP)
- `Thomas Vent - The Performer.mp3` (different track from the wired "One Day Live Your Life")
- `DJ Shimamura feat. MC STONE - VIBES! (ErzRy Artz Remix).mp3`

**Open question for Chris, before this repo goes public on the portfolio**: all of the above are
real commercial/licensed tracks sitting as full audio files in a public GitHub repo. Worth deciding
whether to strip them from git history, replace them with royalty-free placeholders, or accept the
risk as-is (a DMCA takedown or GitHub copyright complaint against a portfolio repo would be a bad
look for a recruiter to stumble on). Not fixed as part of this session, since it is a decision, not
a bug.

## Views

Read from `ViewManager._views` (3 entries) and each `View._viewType`.

| Hierarchy name | `ViewType` enum | Status | Presenting? |
|---|---|---|---|
| View Intro | Unity | Working | Yes |
| View Terrain | Terrain | Working | Yes |
| View Dumbbell | Dumbbell | Broken (reflections/lighting, leftover from an incomplete SRP-to-HDRP upgrade, unrelated to 2026-09-23's work) | Maybe, if there's time to fix it - see gotchas.md |

Note: "View Intro" is the Hierarchy GameObject's name, but its `ViewType` is `Unity` - there is no
separate fourth view. See architecture.md's "Naming trap" note.

Each view owns its own `SpectrumDataContainerModule` (independent audio sampling per view, not a
shared global one). Switching views: Tab (next) / Shift+Tab (previous). Restarting the current
view's audio: R.
