# Audio Visualization

**Status:** Active (Terrain view)  |  **Built:** 2017 to 2019, reworked 2026  |  **Role:** Solo developer, since 2026 together with Claude Code

A sandbox music visualizer for Unity (HDRP): a glowing terrain that is shaped by the song that plays. Bass, mids and highs push
the mesh up, a noise pattern keeps it moving, waves run over it on the beat, and everything can be tuned live in a slide-in menu
with dozens of sliders.

<!-- Intro video (about 20 s, with sound): song Invincible. Starts on the Terrain tab of the menu with the Thunderstorm preset, four quick pattern changes (Voronoi, Triangles, Honeycomb, Tiles), then the menu switches to the Color tab while the terrain morphs into the Jungle preset (mesh morph, new colors), menu closes at the end. Recorded with the scripted video director on the video branch. -->
https://github.com/user-attachments/assets/d5fe2874-6a72-4647-bc8e-21bc63c1d96f

The longer demo is on YouTube: [Audio Visualization, hero video](https://www.youtube.com/watch?v=qF42xZbG59Q)

## What you do here

There is no goal, no score and nothing to win. Put on a song (or the microphone), open the menu and play: pick a noise pattern,
choose which part of the spectrum shapes which part of the terrain, send waves over it on the beat, bend the whole mesh into a
planet, paint it with a palette. Think of it as an instrument for pictures. When you find a look you like, save it as a preset
and come back to it. If you only want to watch, lean back and let a song run.

## Why I built this

This started as a side quest: I wanted a wall in a different project to move along with whatever song was playing. I quickly
realized the same idea could drive all kinds of visuals, so I split it off into its own project and just had fun seeing what I
could build with it. It became a nice outlet for creativity beyond "does the gameplay work", and it still is.

## How it was built

The first version (2017 to 2019) is mine alone: the audio sampling, a small module pipeline for routing audio data into object
properties, and the first visual scenes. In autumn 2026 I picked the project up again and turned it into the Terrain view you
see here, and from that point on I build it with Claude Code. This is a conscious choice: I decided to vibe-code it.

I work as the art director and product owner, Claude Code is the development team. I decide what the project should be and how
it should look and feel, I test every change by eye and ear, and I do the fine tuning that gives it its character. Claude Code
writes the code. That is why there are so many sliders: each one stands for a decision about what looks good, and I know what it
does and when it matters.

## Features

- **Spectrum terrain.** Frequency bands are mapped onto up to three overlapping *audio shapes* (circle, stripes, rays, spiral,
  star, rings and more), bass first. Each layer has its own strength, zoom, invert and blend mode.
- **32 noise patterns** (Perlin, Domain warp, Voronoi, Hexagons, Cubes, Plasma, ...), up to three mixed into one terrain,
  with depth, speed (x and y) and zoom per layer. "Flip beats" turns the whole pattern upside down on the beat, with a smooth morph.
- **Beat waves.** Rings run over the terrain in time with the song's BPM. They start flat on every beat and grow when the bass
  is strong, so quiet passages stay quiet.
- **Mesh morph.** Bends the whole mesh into a planet, tube, dome, pringle, twist or flag, with a beat motion and a slow swing.
- **Color and light.** HDR color palettes (save your own), a color curve with plateau tilt and slope shading so heights stay
  readable, bloom, lens dirt that pulses on the beat, and a background that takes the hue of the terrain.
- **Presets.** Every setting, the test signal settings included, can be saved and loaded as a preset.
- **Camera.** Orbit, move and zoom with the mouse, turn the terrain itself by dragging it, beat-synced zoom and sway.
- **Test signal.** Replaces the song with a calm, fixed spectrum, so an effect can be judged without a moving song. A band
  editor lets you raise and lower single bands with the mouse.
- **Menu that follows your intent.** Windows open on hover, close again when you leave, and the menu slides in before the pointer
  arrives when it moves fast towards the edge. Tick boxes and layer tabs can be switched by a quick dip of the pointer.

Four free-to-use songs, a 20 to 20,000 Hz test sweep and live microphone input, cycled from the player bar or with the arrow keys.
The full list with status and planned ideas is in [docs/features.md](docs/features.md).

<!-- VIDEO 2 (menu, 30-45 s): open the menu, change pattern, shape, palette, a wave. -->
<!-- VIDEO 3 (optional, 15 s): test signal with band editor, or Mesh morph planet. -->

## Controls

Everything can be done with the mouse. The menu on the right opens when the pointer touches the right edge, the player bar
opens at the bottom. The keyboard shortcuts:

| Key | Action |
|---|---|
| Left / Right | Previous / next song |
| Up / Down | Volume (hold) |
| Space or P | Pause and resume (the picture stays) |
| R | Restart the song |
| Tab | Show or hide the terrain menu |
| M or Esc | Show or hide the small menu next to the player bar (HDR output, quit) |
| T | Test signal on / off |
| Left drag | Orbit the camera (on the terrain: turn the terrain) |
| Right drag | Move the camera |
| Mouse wheel | Zoom (on a slider: change the value, Shift = 5x steps) |
| W A S D, Q E | Fly the camera |
| C | Reset camera and terrain turn |

## Under the hood

**Audio.** FFT bins from Unity's spectrum sampler are folded into frequency bands (logarithmic by default, which matches how we
perceive pitch), with optional running-average normalization so quiet and loud tracks look comparable. Every song carries its
own BPM and beat offset; beats are also detected from the bass, so waves and flips work without a map.

**Burst jobs.** Heights, colors, dampening, waves, morph and noise run as Burst jobs on all cores. The mesh update at 115 x 115
faces went from 13.6 ms to 0.7 ms, at 400 x 400 from 33 ms to 5 ms. Triangles and UVs are only rebuilt when they have to be.
Settings like Max FPS, Efficiency (noise only every n-th update, spread over the frames) and the face count are in the menu, with
a green-to-red background that shows what each one costs.

**Frame-rate independent smoothing.** Dampening is time-based, so 60 and 240 FPS look the same and nothing overshoots.

**Color pipeline.** Palette colors go through a glow scale (exposure steps), a saturation curve and a calculated color table
instead of a plain gradient, so bright colors do not wash out and every height keeps a readable slope.

**Data-driven.** Presets and palettes are small JSON files in `StreamingAssets`; the builds ship them and new files go to the
user's data folder.

**Module pipeline (2017 to 2019).** The original source -> transform -> apply system (`VoidToFloatModule`,
`FloatToFloatModule`, ...) is still in the project and still delivers the spectrum and the beat timing to the terrain.
See [docs/architecture.md](docs/architecture.md).

## Where it stands

The Terrain view is the finished first release. The older scenes from before the rework are still in the project but switched
off. Next up: more noise patterns and beat effects, a menu review, per-song audio settings, and long term importing your own
music and recording video from inside the app. The ideas list is in [docs/features.md](docs/features.md).

## Credits

Four songs used in this project are free-to-use releases from [NCS (NoCopyrightSounds)](https://ncs.io/):

- Song: DEAF KEV - Invincible [NCS Release] · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/invincible · Watch: http://youtu.be/J2X5mJ3HDYE
- Song: Lynxie, johnny joined - i still hear your voice · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/istillhearyourvoice · Watch: http://ncs.lnk.to/istillhearyourvoiceAT/youtube
- Song: L3VI - in motion · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/inmotion · Watch: https://ncs.lnk.to/inmotionAT/youtube
- Song: SILVER BULLET - Nobody Else · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/S_NobodyElse · Watch: https://ncs.lnk.to/S_NobodyElseAT/youtube
