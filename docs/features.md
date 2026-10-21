# Terrain View - Feature Overview

What the Terrain view can do today and what is planned. The terrain is a mesh whose vertex heights come from
a noise pattern, the audio spectrum and a few beat effects. Everything below is reachable in the slide-in menu
(Tab) unless noted.

Last updated: 2026-10-21

## Menu tabs

| Tab | Groups |
|---|---|
| Color | Overall glow, background tint, palette dropdown (save / delete own palettes), four gradient colors from low (base) to high with hue, saturation and glow, color from band |
| Terrain | Terrain (face count, height, FPS hint), Noise pattern, Wave, Smoothing, Dampening, Mesh morph |
| Glow | Bloom (threshold, strength, spread), lens dirt (also on the beat) |
| Shapes | Audio shapes (circle, rectangles ...) that map the spectrum onto the terrain |
| Profiles | Save / load / delete profiles, Test signal (signal, level, beat, beat strength, strength from song, freeze noise, edit bands) |

## Features

| Feature | Status | Notes |
|---|---|---|
| Spectrum terrain | done | Bands are cast onto one or more shapes, out-of-bounds handling, smoothing between bands |
| Dampening | done | Separate up / down factors (default 0.05 each), frame-rate independent |
| Noise patterns | done | 27 patterns (Perlin, Simplex, Value, Fractal, Ridged, Billow, Domain warp, Voronoi, Mosaic, Cracks, Rings, Interference, Spiral, Waffle, Hexagons, Bricks, Dots, Pyramids, Flower, Zigzag, Plasma, Tiles, Triangles, Checker, Steps, Cubes, Honeycomb). Start pattern: Domain warp. Speed slider 0 to 10 (half way = 3) |
| Invert per pattern | done | Cell patterns (Voronoi, Cracks, Hexagons, Bricks, Dots, Pyramids, Tiles, Triangles, Cubes) stand up from the ground by default, the Invert toggle flips them. The choice is remembered per pattern |
| Flip on the beat | done | "Flip beats" turns the pattern upside down every N beats, with a smooth morph instead of a hard switch |
| Beat wave | done | Ring waves on the beat. Origin: Middle, Edge, Alternate, Bass edge (starts a little inside of the bass). Mode: Add, Floor, Absorb. Speed is tied to the song's BPM (1 = half the terrain width per beat) |
| Wave trigger | done | Bass has priority, quiet passages stay quiet. A wave starts flat on every beat and grows if the bass gets strong within 0.1 s. Full height at 90 % of the recent loudest bass |
| Color palettes | done | JSON palettes, HDR colors, start palette Lava |
| Saturation / glow mapping | done | Curved saturation slider, weakly saturated colors are dimmed so white does not overpower |
| FPS display + hint | done | Hint to lower the face count when FPS stays under 60 / 30 |
| Test signal | done | Replaces the audio with a calm, fixed spectrum to judge effects without a moving song. "Beat" sends beats on the beat of the playing song (waves and flips react). "Beat strength" lifts all bands on every beat on top of the level, the bass the most; "Strength from song" takes the strength of every beat from the bass of the playing song instead of the slider. "Edit bands" lets the mouse set single bands (left button raises, right button lowers the band under the cursor). The camera beat zoom pauses while it is on. Starts off, not part of a profile |
| Color by band | done | Three sliders (gradient position, brightness, glow) say how much each comes from the activity of the vertex's frequency band instead of its height. 0 = as before |
| Mesh morph | done | Bends the whole mesh after the heights (shape dropdown with previous / next buttons): Bowl, Dome, Planet, Tube, Halfpipe, Saddle, Flag (a wave runs through the mesh), Twist. Amount slider and "On beat" (the mesh is only bent on the beat and relaxes in between) |
| Background tint | done | The sky takes the hue of the base color, brightness stays. Toggle "Tint background" in the Color tab |
| Profiles | done | All terrain settings except the test signal |
| Camera control | done | Mouse (left drag on the background or middle drag orbits, left drag on the terrain turns it around its middle, right drag moves, wheel zooms; both left and right belong to the band editor while it is on) and keyboard (WASD, Q / E, C resets camera and terrain turn, the reset glides), beat zoom, camera menu |

## Planned / ideas

| Idea | Status | Notes |
|---|---|---|
| Per-song audio settings | todo (Chris) | Bass boost, lower outer bands. BPM and offset are set for the new songs, the rest is missing |
| Noise speed variation | idea | One slider, e.g. a slow drift of the noise direction in a circle |
| Zoom per pattern | idea | Each pattern remembers its own zoom |
| More beat-driven invert effects | idea | Breathing, crossfade A to B, zoom / offset kick, bass-driven morph, reverse spiral, half-time ping-pong. Only the plain flip is built |
| More patterns | idea | Stars, scales, weave, more geometric ones |
| Interaction mode | idea | Touch the terrain with the mouse and play on top of a song. The band editor of the test signal is the first step |
| Flip as a wave | idea | "Flip beats" could run as a wave along the audio bands instead of everywhere at once: from the inside out on a circle, from side to side on vertical stripes |
| Menu review | todo | Go over the whole menu once: right modules in the right tabs, right names |
| Color by band: own gradient | idea | A second gradient only for the band activity, or one fixed color per band |
| Band editor: picking with a morph | idea | The editor finds the band on the flat terrain, it is not exact while a morph bends the mesh |
| Import own music, record video | idea | See the portfolio notes |
