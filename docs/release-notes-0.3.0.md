# Release notes 0.3.0

Changes since 0.2.7. Paste this into the GitHub release for 0.3.0.

## New
- **Pattern layers are heights.** Every layer has its own Depth and Mix, plus the modes Replace / Add / Subtract / Highest / Lowest / Multiply / Difference.
- **Five wave patterns:** Waves, Ocean, Ripples, Waveform, Cymatics (32 patterns in total). Honeycomb is now pits by itself.
- **Speed X and Y per pattern** (-10 to 10), zoom remembered per pattern and layer.
- **Vignette slider** in the Glow group (saved with the preset).
- **Color by pattern** sliders in the new Color mapping window, new mesh morph Pillar, and the mesh now glides from the old to the new shape.
- **Round shapes** (circle, spiral, rings) fit the disc at zoom 1.
- **Presets:** "Profiles" are now called presets; new and updated presets Jungle, Lava River, Ocean, Zerg, Thunderstorm, Spacewarp (Circle and Coast are gone). Test signal settings are saved in the preset. Copy setup / Paste setup sends a setup as text.
- **Debug window (F12)** with a live link between Editor and build.
- **HDR follows the monitor:** the HDR switch notices when the window is moved to an HDR monitor (no restart needed); a hint in the menu shows Shift + Win + Left / Right for moving the window.

## Changed
- A zoom or pattern change is now calculated for all points in one update, so there are no grainy ripples.
- Presets of older versions load and look the same (format version 5, automatic conversion).
- Code formatting cleaned up (whitespace only), `.editorconfig` added.

## Known
- Hero and intro video are scripted with the video director, which lives on the `video` branch and is not part of this release.
