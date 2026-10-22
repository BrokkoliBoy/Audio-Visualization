# Terrain View - Feature Overview

What the Terrain view can do today and what is planned. The terrain is a mesh whose vertex heights come from
a noise pattern, the audio spectrum and a few beat effects. Everything below is reachable in the slide-in menu
(Tab) unless noted.

Last updated: 2026-10-21 (Burst jobs, performance settings, seven new audio shapes, spread noise, face count up to 1000, Max FPS, fade in from black, tabs open on hover, color keys share one hue, new gradient key layout, pattern balance, soft top end, area slope)

## Menu tabs

Tabs open on hover (after a short 0.1 s delay, so sweeping across them does not flip through every tab); a click still works.

| Tab | Groups |
|---|---|
| Color | Overall glow, background tint, palette dropdown (save / delete own palettes), four gradient colors from low (base) to high with hue, saturation and glow, color from band |
| Terrain | Terrain (face count, height, FPS hint), Performance, Noise pattern, Wave, Smoothing, Dampening, Mesh morph |
| Glow | Bloom (threshold, strength, spread), lens dirt (also on the beat) |
| Shapes | Audio shapes (circle, square, diamond, stripes, mirrored stripes, rays, spiral, cross, star, rings) that map the spectrum onto the terrain |
| Profiles | Save / load / delete profiles, Test signal (signal, level, beat, beat strength, strength from song, freeze noise, edit bands) |

## Features

| Feature | Status | Notes |
|---|---|---|
| Spectrum terrain | done | Bands are cast onto one or more shapes, out-of-bounds handling, smoothing between bands |
| Audio shapes | done | 12 shapes decide which vertex gets which band (bass first): Circle, Square, Diamond, Stripes Vertical / Horizontal, Mirrored Vertical / Horizontal (bass in the middle line), Rays (bass left and right, highs up and down), Spiral (one and a half turns at zoom 1), Cross (bass along both middle lines), Star (five points, bands follow the star outline, zoom below 1 fills the notches), Rings (repeating, zoom 1 = two rings). Zoom stretches the shape or the repetition, Invert swaps bass and highs |
| Dampening | done | Separate up / down factors (default 0.02 each, slider 0 to 0.12), frame-rate independent: time-based smoothing (1 - (1 - k)^(dt * 240)), so 60 and 240 FPS react the same and nothing overshoots; also used by the audio average and the lerp module |
| Noise patterns | done | 27 patterns (Perlin, Simplex, Value, Fractal, Ridged, Billow, Domain warp, Voronoi, Mosaic, Cracks, Rings, Interference, Spiral, Waffle, Hexagons, Bricks, Dots, Pyramids, Flower, Zigzag, Plasma, Tiles, Triangles, Checker, Steps, Cubes, Honeycomb). Start pattern: Domain warp. Speed slider 0 to 10 (half way = 3) |
| Invert per pattern | done | Cell patterns (Voronoi, Cracks, Hexagons, Bricks, Dots, Pyramids, Tiles, Triangles, Cubes) stand up from the ground by default, the Invert toggle flips them. The choice is remembered per pattern |
| Flip on the beat | done | "Flip beats" turns the pattern upside down every N beats, with a smooth morph instead of a hard switch |
| Beat wave | done | Ring waves on the beat. Origin: Middle, Edge, Alternate, Bass edge (starts a little inside of the bass). Mode: Add, Floor, Absorb. Speed is tied to the song's BPM (1 = half the terrain width per beat) |
| Wave trigger | done | Bass has priority, quiet passages stay quiet. A wave starts flat on every beat and grows if the bass gets strong within 0.1 s. Full height at 90 % of the recent loudest bass |
| Color palettes | done | JSON palettes, HDR colors, start palette Lava |
| Saturation / glow mapping | done | Curved saturation slider, weakly saturated colors are dimmed so white does not overpower |
| FPS display + hint | done | Hint to lower the face count when FPS stays under 60 / 30 |
| Performance | done | Mesh update (calculation only, Editor, Domain warp, 16 cores): 115 x 115 faces 13.6 ms at the start, 3.5 ms after the first pass, 0.7 ms now. 400 x 400 faces: 33 ms before, 5 ms now. Heights, colors, dampening, beat wave and morph run as Burst jobs on all processor cores (TerrainJobs.cs). The noise is calculated in parallel, the band of every vertex per shape is cached until a shape or the face count changes, the audio bands are read once per frame, triangles and uvs are only rebuilt when the face count or the hidden out-of-bounds triangles change |
| Performance settings | done | Group "Performance" in the Terrain tab, part of the profile (except Max FPS). "Noise every" (1 to 8) calculates the noise only every n-th mesh update, in between it blends from the last to the next result. "Spread noise" (only with Noise every above 1) does not calculate the whole pattern in one update, but a scattered n-th of the points in every update, each blending on its own: the work is the same in every frame (300 x 300 faces, every 4th: 4.6 and 1.2 ms alternating before, 2.3 ms every update now) and nothing flows in step. "Max FPS" (30 up to the refresh rate of the monitor, 240 Hz here) limits the frame rate of the whole app, e.g. 60 to save energy while working; it is saved on this computer (PlayerPrefs), not in a profile, and starts at the monitor's refresh rate in a build and at 60 in the Editor, V-Sync off. The mesh is updated every frame. The face count starts at 500 and reaches up to 1000 x 1000 (about 8 ms per mesh update at 500 and 28 ms at 1000, Domain warp, 16 cores), the slider is linear; the high end is for looking at patterns at low FPS. A line shows FPS and the mesh update time in ms. Defaults: 1 / off / monitor rate in a build, 60 in the Editor. Tried and removed again: "Noise step" (noise only for every n-th vertex, interpolated in between), "Smooth noise" (Catmull-Rom instead of straight lines between them) and "Mesh Hz" (limit for the mesh updates) - they confused more than they saved |
| Fade in from black | done | The app starts behind a black cover that holds for 1.5 seconds and then fades in over 2.5 seconds, so the first image comes up from black. There is no loading screen, the start has no real load time |
| Test signal | done | Replaces the audio with a calm, fixed spectrum to judge effects without a moving song. "Level" reaches up to 3 (above 1 the bands overdrive like loud songs, to judge the colors of heights over 1). "Beat" sends beats on the beat of the playing song (waves and flips react). "Beat strength" lifts all bands on every beat on top of the level, the bass the most; "Strength from song" takes the strength of every beat from the bass of the playing song instead of the slider. "Edit bands" lets the mouse set single bands (left button raises, right button lowers the band under the cursor). The camera beat zoom pauses while it is on. Starts off, not part of a profile |
| Color by band | done | Three sliders (gradient position, brightness, glow) say how much each comes from the activity of the vertex's frequency band instead of its height. 0 = as before |
| Gradient layout | done | The height gradient has 7 keys: a dark start (never brighter than exposure -4.5, whatever the base color is), the base color at 16 %, color 2 as a flat area from 28 % to 34 %, color 3 as a flat area from 50 % to 75 %, color 4 reached at the top (100 %). The colors reach the middle of the terrain earlier, so the high colors show more often |
| Pattern balance | done | Slider "Pattern balance" in the Color tab (part of the profile, default 1, 0 = off). A noise pattern that is high or low on average would show only the upper or lower colors of the gradient. The balance measures the average of the pattern (a sample of up to 2048 points) and scales the place in the gradient, so the average height of every pattern lands where a typical one does (0.5 noise, scale limited to 0.5 up to 3). The color only, not the height. It glides when the pattern changes. At the default settings it moves Pyramids up by about a third and Billow down by about a sixth |
| Soft top end | done | Slider "Soft top end" in the Color tab (part of the profile, default 0 = off). Loud bands can push vertices over height 1, and then they all show the last gradient color. With the slider up, the position in the gradient bends smoothly above 60 % instead of ending hard at 1: heights over 1 keep a little variation and the last color is only approached (reached at about the height of 2). The color only, not the height. Not the same as Pattern balance, which looks at the noise average |
| Area slope | done | Slider "Area slope" in the Color tab (part of the profile, default 0.5, 0 = flat as before). The flat color areas of the gradient (colors 2 and 3, two keys of one color) get darker towards their lower end, up to 1.5 exposure steps at 1 (relative to the color, so it works for bright and dark areas alike). The area is no longer one flat color, so the height inside it stays readable without turning up the glow. It runs in one direction, so no double rings like a key in the middle would make |
| Mesh morph | done | Bends the whole mesh after the heights (shape dropdown with previous / next buttons): Bowl, Dome, Planet, Tube, Halfpipe, Saddle, Flag (a wave runs through the mesh), Twist. Amount slider and "On beat" (the mesh is only bent on the beat and relaxes in between) |
| Background tint | done | The middle and bottom of the sky take the hue of the base color, brightness and saturation come from the sky colors in the volume profile. Toggle "Tint background" in the Color tab |
| Profiles | done | All terrain settings except the test signal |
| Camera control | done | Mouse (left drag on the background or middle drag orbits, left drag on the terrain turns it around its middle, right drag moves, wheel zooms; both left and right belong to the band editor while it is on) and keyboard (WASD, Q / E, C resets camera and terrain turn, the reset glides), beat zoom, camera menu |

## Planned / ideas

| Idea | Status | Notes |
|---|---|---|
| More beat-driven invert effects | idea (next) | Breathing, crossfade A to B, zoom / offset kick, bass-driven morph, reverse spiral, half-time ping-pong. Only the plain flip is built |
| More patterns | idea (next) | Weave, stars, scales, more geometric ones |
| More audio shapes | idea | Built: mirrored stripes, rays, spiral, cross, star, rings. Left: offset center (needs two more sliders), bands that follow the noise (the band map would change every frame, so the cache would not help). The four corner shapes were removed (2026-10-21) |
| Per-song audio settings | todo (Chris) | Bass boost, lower outer bands. BPM and offset are set for the new songs, the rest is missing |
| Menu review | todo | Go over the whole menu once: right modules in the right tabs, right names |
| Flip as a wave | idea | "Flip beats" could run as a wave along the audio bands instead of everywhere at once: from the inside out on a circle, from side to side on vertical stripes. Gets relevant once more shapes show where the flip starts |
| Color by band: own gradient | idea | A second gradient only for the band activity, or one fixed color per band |
| Pattern morph slider | idea | A pattern morph slider that blends the pattern upside down and back, flat in the middle. Not decided yet |
| Noise as a Burst job | idea | The noise is about 90 % of the rest of the mesh update at high face counts. A Burst version of the most used patterns would cut it further |
| Different resolution per aspect | idea | Audio shape at low resolution, noise and waves at high resolution, or detail only where it is needed |
| HDRP tessellation | to check | Whether HDRP tessellation in Unity 6.6 could give smooth surfaces on a low face count |
| Noise speed variation | parked | One slider, e.g. a slow drift of the noise direction in a circle. The current look is good as it is |
| Band editor: picking with a morph | idea | The editor finds the band on the flat terrain, it is not exact while a morph bends the mesh |
| Color noise separate from the height | far back | Much of the look comes from the color following the height, so this is far down |
| Interaction mode | long term | Touch the terrain with the mouse and play on top of a song. The band editor of the test signal is the first step |
| Import own music, record video | long term | See the portfolio notes |
