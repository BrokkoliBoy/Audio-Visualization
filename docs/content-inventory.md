# Content reference: songs and views

The songs and views wired into the scene.

## Songs

Six clips live under `Game/Audio/Audio Clips/`. They are cycled with the Left / Right arrow keys or the player bar
(`SimpleControl`), whichever view is shown.

| Song | Artist | BPM |
|---|---|---|
| In Motion | L3VI | 134 |
| Invincible | DEAF KEV | 100 |
| i still hear your voice | johnny joined, Lynxie | 134 |
| Nobody Else | Silver Bullet | 140 |
| 20 to 20,000 Hz sweep | none | test tone, not a song |
| *(Microphone input)* | none | live, no fixed BPM |

The four songs are free-to-use [NCS (NoCopyrightSounds)](https://ncs.io/) releases. The required attribution is in the
Credits section of the [README](../README.md).

## Views

One view is in the `ViewManager` list: **View Terrain**, the main content of the project (see [features.md](features.md)).
The scene also holds **View Intro**, **View Unity** (both `ViewType.Unity`) and **View Dumbbell**. They are intact but not in
the `ViewManager` list, so they are never shown: the release is the terrain only (decided 2026-10-21), and the Dumbbell's
lighting predates HDRP (see [gotchas.md](gotchas.md)). R restarts the song.

Each view owns its own `SpectrumDataContainerModule`, so the audio is sampled per view and not through one global instance.
