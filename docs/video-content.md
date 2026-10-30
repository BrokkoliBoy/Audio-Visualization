# Video content (branch `video` only)

What the video takes use: presets, palettes and the special settings of the morphs. The code is `Assets/VideoDirector/`, the files are in `Assets/StreamingAssets/Presets/`. Keep this file current whenever a video preset, palette or timeline setting changes.

Rule: every video uses only its own copies of presets and palettes (hero: prefix `Hero-Video`, intro: prefix `Intro`), never the files of `next`. The ones a finished video really uses carry the suffix ` Live`; unused leftovers have none.

## Hero video (`HeroVideoTimeline`, tag `hero-video-v1` is the state as recorded on 30.10.2026)

Song Invincible (100 BPM), beat 0 to 68, fade out from beat 60.

### Terrain presets

| Preset | Used for |
|---|---|
| Hero-Video Spacewarp Live | Start of the take; source of the first morph. Detail 500. |
| Hero-Video Thunderstorm Live | Target of the first morph (beats 21.5 to 27.5, switch at 24.5). Detail 500. |
| Hero-Video Jungle Live | Target of the second morph (beats 30.5 to 38.5, switch at 34.5); base of the live part. Detail 400. |
| Hero-Video Ocean | Not used (earlier test). |

### Color palettes

Each preset has the palette of the same name (`ColorPaletteName`). The timeline also switches palettes in the live part:

| Palette | Used for |
|---|---|
| Hero-Video Spacewarp Live | Palette of the Spacewarp preset; again in the live part at beat 46.5. |
| Hero-Video Thunderstorm Live | Palette of the Thunderstorm preset; in the live part at beat 42.5. Values are those of the shipped palette Thunderstorm (ColorShift 0.2), as the recording had them. |
| Hero-Video Jungle Live | Palette of the Jungle preset. |
| Hero-Video Lava River Live | Live part at beat 50.5 (copy of the shipped Lava River). |
| Hero-Video Ocean | Not used. |

### Special settings of the morphs

Default of a morph (`DirectorTimeline.Morph`): all numbers glide eased from the old to the new preset, patterns, switches, shapes and mesh detail change at the middle (switch at 50 %), mesh shape glides for 1 s, audio shape changes at the switch.

Spacewarp to Thunderstorm (6 beats, 3 in and 3 out, starts 3 beats after the drop at beat 18.5):
- Mesh morph shape starts 0.825 beats after the start and glides for 2.3 s.
- Pattern zoom moves 20 % first, 80 % around the switch, then slowly to the end.
- Audio shape zoom keeps its start value until the switch.
- Terrain height sinks to 0.2 around the switch.
- Wave swing size goes to 0 at the switch.
- Pattern hue, band hue, pattern glow and floor share go to 0 around the switch (slow down, steep at the switch, back in about 0.9 beats).

Thunderstorm to Jungle (8 beats, starts 9 beats after the start of the first morph):
- Audio shape becomes Square at once when the morph starts; the shape of Jungle follows at the switch.
- Pattern zoom stays for 3 beats, then glides smoothly over the remaining 5.
- Mesh morph shape starts 1.1 beats after the start and glides for 3.1 s.
- Terrain height sinks to 0.2 around the switch.

### Live part (beats 42.5 to 60)

A pattern change every 2 beats, each with its own zoom, depth, scroll speed and layer 2: Mosaic (42.5), Hexagons (44.5), Zigzag (46.5), Dots (48.5), Bricks (50.5), Cracks (52.5), Ripples (54.5), Waveform (56.5). What a pattern leaves out falls back to the value of Hero-Video Jungle Live. Palettes switch at 42.5, 46.5 and 50.5 (see above).

## Intro video (`IntroVideoTimeline`, first version, to be tuned)

Song Invincible, beat 15 to 42.5 (16.5 s), then the song is paused like with the space key and the picture stands still (cut in DaVinci Resolve). Menu open from the start on the Terrain tab with its windows folded (only the pattern stays visible), switches to the Color tab at beat 30.5 (two beats after the morph starts) and closes at beat 38.5. F5 switches between the Hero and Intro profile while no take runs (`DirectorProfiles`).

| Preset / palette | Used for |
|---|---|
| Intro Thunderstorm Live | Start of the take (copy of Hero-Video Thunderstorm Live, palette of the same name). |
| Intro Jungle Live | Target of the morph (copy of Hero-Video Jungle Live, palette of the same name). |

- Drop at beat 18.5, then a pattern change every 2 beats: Voronoi (20.25, zoom 5 (slider number), depth 0.7), Interference (22.25, zoom 7, depth 0.66), Bricks (24.25, zoom 7.2), Tiles (26.25, zoom 8; a quarter beat before the beat, the new pattern needs a moment to become visible). Invert is off for all of them, only the start pattern of Intro Thunderstorm Live is inverted.
- Morph Thunderstorm to Jungle over 8 beats (28.5 to 36.5, switch at 32.5) with the same special settings as the second morph of the hero video (copied code, two places to maintain): Square at once, pattern zoom late glide 3 beats, mesh shape from 1.1 beats for 3.1 s, terrain height 0.2.
