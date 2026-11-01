# Project overview

A short map of the whole project for anyone (a person or a Claude chat) who has to decide what to show, what to name
and where to look. It says what is in the repository, what ships in the build and what does not. Details live in
[features.md](features.md) (what the terrain can do), [architecture.md](architecture.md) (how the code is built),
[content-inventory.md](content-inventory.md) (songs and views) and [gotchas.md](gotchas.md) (traps).

Last checked against the code: 2026-11-01 (version 0.3.0, branch `next`).

## What it is

A music visualizer for Unity 6 (6000.6.2f1, HDRP). A glowing terrain mesh is shaped by the song: audio bands, a noise
pattern and beat effects set the heights, colors come from a palette, and a slide-in menu with dozens of sliders tunes
everything live. There is no goal and no score; it is an instrument for pictures. Setups are saved as presets.

It started in 2017 to 2019 as a small audio-to-property pipeline plus a few scenes. In autumn 2026 it was picked up again
and the terrain was built; since then it is developed with Claude Code (Chris is art director and product owner, tests
by eye and ear, Claude Code writes the code).

## What ships and what does not

Only the **terrain** is shown and shipped as the thing you can use. Everything else is either hidden or kept out of the
release on purpose.

| Part | In the build | Notes |
|---|---|---|
| Terrain (the visualizer, its menu, presets, palettes, camera, test signal) | yes, the only view | `Assets/Views/View Terrain/`, about 7,800 lines of C# plus the shared menu code in `Assets/Core/Scripts/UI/` (about 4,200 lines) |
| Songs: 4 NCS releases, a 20 to 20,000 Hz sweep, live microphone | yes | See content-inventory.md; the sweep is a test tone, not a song |
| 8 presets, 7 palettes in `StreamingAssets/Presets` | yes | Default, Jungle, Lava River, Ocean, Spacewarp, Star, Thunderstorm, Zerg (Default has no palette file of its own) |
| Debug window (F12, live link Editor to build, button for the Screenshots folder) | yes, hidden | Only for development; Ctrl+F12 turns it on in any build |
| Older views: View Intro, View Unity, View Dumbbell | in the scene, switched off | Not in the `ViewManager` list, so never shown. Dumbbell's lighting predates HDRP (gotchas.md). `Assets/Views/View Test` is an unused shader experiment |
| Module pipeline (2017 to 2019: source, transform, apply) | yes, in the code | Still delivers the spectrum and the beat timing to the terrain; the camera beat zoom is still a chain of modules. About 2,600 lines |
| Video Director (scripted takes for the demo videos) | no | Lives only on the branch `video` (`Assets/VideoDirector/`), never merged into `next` or `main` |

The project has exactly one view, so the README and the menus should not give it a name like "Terrain view". The
code still calls it `View Terrain` and `ViewTypeEnum.Terrain`; that is an internal name.

## How the terrain works, in five lines

1. The audio sampler turns FFT bins into frequency bands (logarithmic by default); each song carries its own BPM and beat
   offset, and beats are also detected from the bass.
2. Up to three **audio shapes** (circle, square, diamond, stripes, mirrored stripes, rays, spiral, cross, star, rings)
   decide which vertex gets which band, bass first.
3. Up to three **noise pattern layers** (32 patterns) are heights with their own depth, mix and mode; speed, zoom and
   invert per layer. "Flip beats" turns the mix upside down on the beat.
4. **Beat waves** (rings on the beat) and a **mesh morph** (planet, twist, tube, pillar, dome, pringle, flag) are applied
   on top; heights, colors, dampening, waves and morph run as Burst jobs.
5. Colors come from a calculated color table (palette, glow scale, plateau tilt, slope shading), plus bloom, lens dirt,
   vignette and a background tint. Everything is saved in presets (JSON files).

## The menu

Four tabs: **Color** (palette and the four color windows), **Light** (gradient, lighting, color mapping, glow),
**Terrain** (terrain, noise pattern, dampening, audio shape, beat wave, mesh morph), **General** (presets, camera,
performance, test signal, menu navigation). The menu opens when the pointer touches the right screen edge (or with Tab),
the player bar at the bottom edge, a small menu (HDR output, quit) with M or Esc. Windows open on hover or by click, and
the pointer can pick tick boxes and layer tabs by a quick dip. Hotkeys: arrow keys (song, volume), Space / P pause,
R restart, T test signal, W A S D Q E fly, C reset camera, F2 screenshot, Shift+F2 with the menu (PNG in `Screenshots/` of the data folder,
named after the loaded preset), F12 debug.

## Where things are

| Looking for | Place |
|---|---|
| Terrain mesh, jobs, noise, waves, morph | `Assets/Views/View Terrain/Scripts/` (`TerrainGenerator`, `TerrainJobs`, `TerrainNoise`, `TerrainBeatWave`) |
| Presets and palettes (code) | `Assets/Views/View Terrain/Scripts/Settings/` |
| Menu windows of the terrain | `Assets/Views/View Terrain/Scripts/UI/` (one `TerrainGroupUI...` per window) |
| Shared menu behavior (hover, fold, dip, wheel, accents) | `Assets/Core/Scripts/UI/` |
| Audio sampling, player, test signal, shapes | `Assets/Core/Scripts/Game/Audio/` |
| Module pipeline | `Assets/Core/Scripts/Game/Modules/`, `.../Modifiables/` |
| Views | `Assets/Core/Scripts/Game/Views/` (`ViewManager`), scene `Assets/Core/Scenes/Scene - Views.unity` |
| Preset and palette files | `Assets/StreamingAssets/Presets/` (user files go to the persistent data path) |
| README pictures (menu, preset gallery, JPGs under 400 KB) | `docs/images/` |
| Release notes | `docs/release-notes-0.3.0.md` |

## Repository and release

- GitHub: `BrokkoliBoy/Audio-Visualization`. Branches: `main` (releases, tags 0.1.0, 0.1.1, 0.2.7 so far), `next`
  (development, 33 commits ahead of `main`), `video` (Video Director, takes, video presets). Version 0.3.0 is set in the
  Player Settings (company name Gavi Gameworks), not yet merged to `main`.
- License: MIT for the source code; the four songs, the test sweep and third-party packages are not covered.
- The private repo `AudioVisualization` is archived and not developed any more (it holds the original songs).
- The public README is Claude's first version being finalized by Chris. It has no Learnings section and gets none.

## What is not built yet

The open ideas (more beat effects, more patterns, per-song audio settings, menu review, new views, importing music,
recording video) are collected in the "Planned / ideas" table of [features.md](features.md).
