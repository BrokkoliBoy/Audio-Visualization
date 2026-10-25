# Content Reference — Songs & Views

A reference of the songs and views currently wired into the scene.

## Songs

Six clips live under `Game/Audio/Audio Clips/`, cycled independently of the current view with the
Left / Right arrow keys or the player bar (`SimpleControl`).

| Song | Artist | BPM |
|---|---|---|
| In Motion | L3VI | 134 |
| Invincible | DEAF KEV | 100 |
| i still hear your voice | johnny joined, Lynxie | 134 |
| Nobody Else | Silver Bullet | 140 |
| 20 – 20,000 Hz sweep | — | test tone, not a song |
| *(Microphone input)* | — | live, no fixed BPM |

The four songs are free-to-use [NCS (NoCopyrightSounds)](https://ncs.io/) releases — see the
Credits section in the root README for the required attribution.

## Views

One view is in the `ViewManager` list: **View Terrain**, the main content of the project (see
features.md). The scene also holds **View Intro** and **View Unity** (both `ViewType.Unity`) and
**View Dumbbell**, fully intact but deliberately not in the `ViewManager`, so they are never
shown (decided 2026-10-21: the release is Terrain only; the Dumbbell's lighting predates HDRP, see
gotchas.md). Restarting the song: R.

Each view owns its own `SpectrumDataContainerModule` (independent audio sampling per view, not a
shared global one).
