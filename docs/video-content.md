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

## Intro video

Planned, not built (checklist C45): about 15 s, Thunderstorm to Jungle morph over 8 beats, own copies with the prefix `Intro`.
