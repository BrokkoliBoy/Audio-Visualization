# Terrain View - Feature Overview

What the Terrain view can do today and what is planned. The terrain is a mesh whose vertex heights come from
a noise pattern, the audio spectrum and a few beat effects. Everything below is reachable in the slide-in menu
(Tab) unless noted.

Last updated: 2026-10-21

## Menu tabs

| Tab | Groups |
|---|---|
| Color | Overall glow, palette dropdown (save / delete own palettes), four gradient colors from low (base) to high with hue, saturation and glow |
| Terrain | Terrain (face count, height, FPS hint), Noise pattern, Wave, Smoothing, Dampening |
| Glow | Bloom (threshold, strength, spread), lens dirt (also on the beat) |
| Shapes | Audio shapes (circle, rectangles ...) that map the spectrum onto the terrain |
| Profiles | Save / load / delete profiles, Test signal |

## Features

| Feature | Status | Notes |
|---|---|---|
| Spectrum terrain | done | Bands are cast onto one or more shapes, out-of-bounds handling, smoothing between bands |
| Dampening | done | Separate up / down factors (default 0.05 each), frame-rate independent |
| Noise patterns | done | 22 patterns (Perlin, Simplex, Value, Fractal, Ridged, Billow, Domain warp, Voronoi, Mosaic, Cracks, Rings, Interference, Spiral, Waffle, Hexagons, Bricks, Dots, Pyramids, Flower, Zigzag, Plasma, Tiles). Start pattern: Domain warp |
| Invert per pattern | done | Cell patterns (Voronoi, Hexagons, Bricks, Dots, Pyramids, Tiles) stand up from the ground by default, the Invert toggle flips them. The choice is remembered per pattern |
| Flip on the beat | done | "Flip beats" turns the pattern upside down every N beats, with a smooth morph instead of a hard switch |
| Beat wave | done | Ring waves on the beat. Origin: Middle, Edge, Alternate, Bass edge. Mode: Add, Floor, Absorb. Speed is tied to the song's BPM (1 = half the terrain width per beat) |
| Wave trigger | done | Bass has priority, quiet passages stay quiet. A wave starts flat on every beat and grows if the bass gets strong within 0.1 s. Full height at 90 % of the recent loudest bass |
| Color palettes | done | JSON palettes, HDR colors, start palette Lava |
| Saturation / glow mapping | done | Curved saturation slider, weakly saturated colors are dimmed so white does not overpower |
| FPS display + hint | done | Hint to lower the face count when FPS stays under 60 / 30 |
| Test signal | done | Replaces the audio with a calm, fixed spectrum (and an optional kick at a set BPM) to judge effects without a moving song. Starts off, not part of a profile |
| Profiles | done | All terrain settings except the test signal |
| Camera control | done | Mouse and keyboard, beat zoom, camera menu |

## Planned / ideas

| Idea | Status | Notes |
|---|---|---|
| Per-song audio settings | todo (Chris) | Bass boost, lower outer bands. BPM and offset are set for the new songs, the rest is missing |
| Background color from the base color | idea | Tint the upper (reddish) part of the sky with the base hue, not the brightness. Maybe a menu option instead |
| Noise speed variation | idea | One slider, e.g. a slow drift of the noise direction in a circle |
| Zoom per pattern | idea | Each pattern remembers its own zoom |
| More beat-driven invert effects | idea | Breathing, crossfade A to B, zoom / offset kick, bass-driven morph, reverse spiral, half-time ping-pong. Only the plain flip is built |
| Test signal: band editor | idea | Click bands to set their level (left / right mouse button) |
| Interaction mode | idea | Touch the terrain with the mouse, optionally on top of a song |
| Test the beat flip with the test signal | todo | Not tried yet in the calm setup |
| Import own music, record video | idea | See the portfolio notes |
