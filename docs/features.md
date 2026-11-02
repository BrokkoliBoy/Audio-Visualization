# Feature overview

What the visualizer can do today and what is planned. The terrain is a mesh whose vertex heights come from a noise
pattern, the audio spectrum and a few beat effects. Everything below is reachable in the slide-in menu (Tab) unless noted.
Checked against the code on 2026-11-02.

## Menu tabs

Four tabs. They open on hover at once, a click still works.

| Tab | Windows |
|---|---|
| Color | Palette (dropdown, save / delete own palettes) and one window each for the Base, Low, Mid and High color (hue, saturation, glow) |
| Light | Gradient (color shape and balance), Lighting (height contrast, slope shading), Color mapping (color by band, color by pattern), Glow (brightness, bloom, lens dirt, vignette, background tint) |
| Terrain | Terrain (detail, height), Noise pattern (up to three layers), Dampening, Audio shape (up to three layers), Beat wave, Mesh morph |
| General | Presets, Camera, Performance, Test signal, Menu navigation |

## Features

| Feature | Notes |
|---|---|
| Spectrum terrain | The frequency bands, bass first, are cast onto up to three overlapping audio shapes. Each shape layer has its own strength, zoom, invert and blend operation; layers 2 and 3 start off |
| Audio shapes | 12: Circle, Square, Diamond, Vertical / Horizontal stripes, Mirrored vertical / horizontal stripes, Rays, Spiral, Cross, Star, Rings. Circle, Spiral and Rings are round and fit the disc at zoom 1 |
| Noise patterns | 32 (Perlin, Simplex, Value, Fractal, Ridged, Billow, Domain warp, Voronoi, Mosaic, Cracks, Rings, Interference, Spiral, Waffle, Hexagons, Bricks, Dots, Pyramids, Flower, Zigzag, Plasma, Tiles, Triangles, Checker, Steps, Cubes, Honeycomb, Waves, Ocean, Ripples, Waveform, Cymatics). The start pattern is Domain warp. Per pattern: speed in x and y (-10 to 10), its own zoom range, and an Invert switch |
| Pattern layers | Up to three patterns are mixed. A pattern is a height (high in the pattern is high in the terrain). Every layer has its own pattern, Depth, zoom, speed and Invert. Layers 2 and 3 also have an on switch, a Mode (Replace, Add, Subtract, Highest, Lowest, Multiply, Difference) and a Mix |
| Flip beats | Turns the pattern upside down every N beats, with a smooth morph instead of a hard switch |
| Dampening | Separate up and down factors (default 0.02 each), frame-rate independent, so 60 and 240 FPS react the same and nothing overshoots |
| Beat waves | Rings that run over the terrain on the beat. Origin: Middle, Edge, Alternate, Bass edge. Mode: Add, Floor, Absorb, Subtract, Minimum. The speed is tied to the song's BPM, "Every n-th beat" (1 to 8) starts a wave only on every n-th beat |
| Wave trigger | The bass has priority, quiet passages stay quiet. A wave starts flat on every beat and grows if the bass gets strong within 0.1 s |
| Mesh morph | Bends the whole mesh after the heights: Planet, Twist, Tube, Pillar, Dome, Pringle, Flag. The mesh glides from the old to the new shape. Beat motion (beat boost, rise time, fall time, springiness) and a slow swing in time with the beats; the window starts folded |
| Color palettes | JSON files with HDR colors. The glow runs from 0 (nearly black) over 3 (the plain color) to 10 (+6 exposure steps) |
| Color shaping | The color table follows the palette: Floor glow, Color shift, Color blending, Plateau tilt, Slope shading, Tilt mode, Hue balance, Pattern leveling (evens out the average height of the patterns in the gradient) and Soft top end (loud bands over height 1 keep some variation) |
| Color mapping | Color by band and Color by pattern: three sliders each (Hue, Saturation, Glow) say how much each comes from the band activity or the pattern strength at the vertex instead of the height |
| Bloom, lens dirt, vignette, tint | Bloom with strength, lens dirt that pulses on the beat, vignette (default 0.25), and a background that takes the hue of the base color (Tint background) |
| Presets | All terrain settings, including the test signal settings (but not whether the signal is on), plus all colors. The app starts with the preset of the last session and asks before closing with unsaved changes. Picking a preset in the dropdown loads it, with a preview while the list is open. Copy setup / Paste setup sends a setup as text |
| Test signal | Replaces the audio with a calm, fixed spectrum (key T), so effects can be judged without a moving song. Level up to 3, beat strength, strength from song, freeze noise, and a band editor (left button raises, right button lowers the band under the cursor). The settings are part of the preset |
| Camera | Left drag orbits (on the terrain: turns it), right drag moves, wheel zooms, W A S D / Q E fly, C resets. Beat zoom and camera sway can be switched on or off |
| Pause keeps the picture | Space / P freezes the spectrum and the terrain time, so the picture stays; on resume it goes on without a jump |
| Performance | The mesh update (115 x 115 faces) went from 13.6 ms to 0.7 ms. Heights, colors, dampening, waves and morph run as Burst jobs on all cores. Detail goes up to 1000 x 1000 faces. Max FPS, Efficiency and a face count readout have a green-to-red background that shows what a setting costs; a hint appears when the speed is too high for the frame rate |
| HDR output | Toggle in the small menu (M / Esc). The app checks about once a second whether the monitor offers HDR, so moving the window (Shift + Win + Left / Right) to an HDR monitor enables HDR without a restart, unless it was switched off by hand |
| Menu behavior | The menu opens when the pointer approaches the right edge, the player bar at the bottom, before the pointer arrives. Windows open on hover or by click, can be pinned or folded (right click on the header folds). Tick boxes and layer tabs can be switched by a quick dip of the pointer. The mouse wheel changes sliders, tick boxes and dropdowns (Shift = 5 times larger steps). The Menu navigation window switches these behaviors on and off |
| Look of the menu | Every group has a colored stripe at its header, the hues run through the gradient of its tab. Slider labels shrink to fit. Numbers are always shown the English way (1.5) |
| Start | The app starts behind a black cover that holds for 1.5 s and fades in over 2.5 s. There is no loading screen |
| Screenshot key | F2 saves the camera picture as a PNG without the menu, Shift + F2 with the menu (the file name ends in `_Menu`). The folder is `Screenshots` in the data path (`%USERPROFILE%\AppData\LocalLow\Gavi Gameworks\Audio Visualization\Screenshots`), the name is the preset and the time. The colors are only right with HDR output off |
| Debug window | Only in the Editor, in a development build, or after Ctrl+F12 in any build. F12 opens it. A switch lets a build read and write the preset files of the Unity project, a button opens the Screenshots folder, and a live link passes setting changes between the Editor and a build. Code in `Assets/Core/Scripts/DebugTools/` |

## Planned / ideas

| Idea | Status | Notes |
|---|---|---|
| More beat-driven invert effects | idea | Breathing, crossfade A to B, zoom or offset kick, bass-driven morph. Only the plain flip is built |
| More patterns | idea | Weave, stars, scales, more geometric ones |
| More audio shapes | idea | Offset center, bands that follow the noise |
| Per-song audio settings | todo | Bass boost, lower outer bands. BPM and offset are set for the four songs, the rest is missing |
| Menu review | todo | Go over the whole menu once: right windows in the right tabs, right names |
| Flip as a wave | idea | "Flip beats" could run as a wave along the audio bands instead of everywhere at once |
| Color by band: own gradient | idea | A second gradient only for the band activity, or one fixed color per band |
| Pattern morph slider | idea | Blends the pattern upside down and back, flat in the middle. Not decided |
| Different resolution per aspect | idea | Audio shape at low resolution, noise and waves at high resolution |
| HDRP tessellation | to check | Whether tessellation in Unity 6.6 could give smooth surfaces on a low face count |
| Band editor with a morph | idea | The editor finds the band on the flat terrain, it is not exact while a morph bends the mesh |
| Interaction mode | long term | Touch the terrain with the mouse and play on top of a song. The band editor is the first step |
| Import own music, record video | long term | |
| Undo / redo | far back | History of preset snapshots. Not wanted for now |
| Layer from below | idea | Per layer a switch "from below (Height) / from above (Depth)". Too much work for now |
| Band-driven pattern strength | idea | Mix or Depth follows the band value at the vertex |
| Pattern cross-fade | idea | Blend the old and the new pattern for smooth pattern changes |
| Dust (VFX Graph) | idea | Subtle audio-reactive dust. A first version exists on the branch `video` only |
| Depth without the beat | idea | Pattern Depth driven by loudness, a slow wave or a crossfade between layers |
