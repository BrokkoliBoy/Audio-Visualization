# Release notes 0.3.0

Release title: `Audio Visualization 0.3.0 (Windows)`. Tag: `0.3.0` (the earlier tags have no `v`). Paste everything below the line into the GitHub release.

---

A glowing terrain that is shaped by the song that plays, with dozens of sliders to tune the look live. This release adds a lot of new patterns and presets, a round of polish on colors, waves and the menu, and HDR that follows the monitor.

## Download

Download the zip below, extract it, and run `Audio Visualization.exe` inside. Windows 64-bit only, about 80 MB.

Windows may show a SmartScreen warning because the executable is not signed. Choose "More info" and then "Run anyway".

The app is made for HDR monitors and switches HDR on by itself if the monitor and Windows support it. It also runs on a normal monitor.

## What's new since 0.2.7

- **Pattern layers are heights.** Every layer has its own Depth and Mix, plus the modes Replace / Add / Subtract / Highest / Lowest / Multiply / Difference.
- **Five wave patterns:** Waves, Ocean, Ripples, Waveform, Cymatics (32 patterns in total). Honeycomb is now pits by itself.
- **Speed X and Y per pattern** (-10 to 10), zoom remembered per pattern and layer.
- **Vignette slider** in the Glow group (saved with the preset).
- **Color by pattern** sliders in the new Color mapping window, new mesh morph Pillar, and the mesh now glides from the old to the new shape.
- **Round shapes** (circle, spiral, rings) fit the disc at zoom 1.
- **Presets:** "Profiles" are now called presets; new and updated presets Jungle, Lava River, Ocean, Zerg, Thunderstorm, Spacewarp and Star, with a palette of its own for Star (Circle and Coast are gone, eight presets in total). Test signal settings are saved in the preset. Copy setup / Paste setup sends a setup as text.
- **Screenshot key:** F2 saves a PNG of the camera picture, Shift + F2 with the menu in it (the file name ends in `_Menu`). The files go to the `Screenshots` folder in the data folder, named after the loaded preset; the debug window has a button that opens the folder. The colors are only right with HDR output switched off.
- **Debug window (F12)** with a live link between Editor and build.
- **HDR follows the monitor:** the HDR switch notices when the window is moved to an HDR monitor (no restart needed); a hint in the menu shows Shift + Win + Left / Right for moving the window.

## Changed
- A zoom or pattern change is now calculated for all points in one update, so there are no grainy ripples.
- Presets of older versions load and look the same (format version 5, automatic conversion).
- Code formatting cleaned up (whitespace only), `.editorconfig` added.


## Known
- The hero and intro videos are scripted with a video director that lives on the `video` branch and is not part of this release.

## More

How the app works and what it can do is in the [README](https://github.com/BrokkoliBoy/Audio-Visualization#readme).

## Credits

Songs from NCS (NoCopyrightSounds):

- Song: DEAF KEV - Invincible [NCS Release] · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/invincible · Watch: http://youtu.be/J2X5mJ3HDYE
- Song: Lynxie, johnny joined - i still hear your voice · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/istillhearyourvoice · Watch: http://ncs.lnk.to/istillhearyourvoiceAT/youtube
- Song: L3VI - in motion · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/inmotion · Watch: https://ncs.lnk.to/inmotionAT/youtube
- Song: SILVER BULLET - Nobody Else · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/S_NobodyElse · Watch: https://ncs.lnk.to/S_NobodyElseAT/youtube
