# Content Reference — Songs & Views

A reference of the songs and views currently wired into the scene.

## Songs

Six clips live under `Game/Audio/Audio Clips/`, cycled independently of the current view with the
Left / Right arrow keys or the on-screen menu (`SimpleControl`).

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

Two views are in the active V / Shift+V rotation: **View Terrain** (first) and **View Intro**
(`ViewType.Unity`). A third, **View Dumbbell**, exists fully intact in the scene but is intentionally
excluded from the rotation for now (see gotchas.md). Restarting the current view's audio: R.

Each view owns its own `SpectrumDataContainerModule` (independent audio sampling per view, not a
shared global one).
