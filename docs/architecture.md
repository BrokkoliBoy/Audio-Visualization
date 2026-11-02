# Architecture

A music visualizer. The main content is the **terrain** (2026): a mesh whose heights come from a noise pattern, the audio
spectrum and a few beat effects, tuned live in a slide-in menu. Underneath sits the **module pipeline** from 2017 to 2019,
a lightweight stand-in for visual scripting: chains of "pull a float out of the audio spectrum, transform it, apply it to a
component property", wired together by dragging components onto GameObjects. The terrain generator only uses the pipeline's
audio entry point (`SpectrumDataContainerModule`) and the song's BPM; the camera beat zoom is still a pipeline chain. The
full feature list is in [features.md](features.md).

The namespace is `Gavi`, split into `Gavi.Audio`, `Gavi.Modules.*`, `Gavi.Modifiables`, `Gavi` (views), `Gavi.Utility` and
`Gavi.Events`.

## The pipeline: source -> transform -> apply

Every node is a `Module` (`Assets/Core/Scripts/Game/Modules/Module.cs`), a `MonoBehaviour` base class that deliberately
does not implement `Update`, so a module that needs no per-frame work pays nothing for it. A module also names its own
GameObject (`GetName()`, shown in the Hierarchy), so a pipeline graph labels itself.

Three families of modules match the three stages:

1. **Source** (`Game/Modules/Audio Modules/`): pull data out of the audio system with no upstream module, named
   `VoidToXModule` (e.g. `SpectrumDataContainerModule : VoidToFloatArrayModule`). The only place the pipeline touches
   `Gavi.Audio`.
2. **Transform** (`Game/Modules/Math Modules/`): organized by output type (`Float`, `Int`, `Color`), then by conversion
   shape: `FloatToFloatModules` (clamp, decay, lerp, remap, add, multiply, ...), `FloatArrayToFloatModules`,
   `VectorModules`, `FunctionModules` and more. Each module points at the upstream module it reads from, so a chain is a
   row of GameObjects that each reference the previous one.
3. **Apply** (`Game/Modifiables/`): plain `MonoBehaviour`s, one per kind of target: `MaterialPropertyModifiable`,
   `LightModifiable`, `CameraModifiable`, `PositionModifiable`, `RotationModifiable`, `ScaleModifiable`,
   `VFXGraphModifiable`, `OrganicMovementModifiable`. Each holds a list of `(propertyId, Module)` pairs and calls
   `Module.GetValue()` every frame to push the value onto the real Unity property.

## Audio: sampling and playback

- **`AudioPlayer`** (`Game/Audio/AudioPlayer.cs`): singleton, owns the one `AudioSource`. `Play(AudioClipContainer)` swaps
  between a music mixer group and a microphone group (`UseMic`); the microphone starts a `Microphone.Start` loop clip.
- **`AudioClipContainer`**: wraps one song: the clip, BPM, beat offset, an optional beat map (`FunctionModule`), an optional
  per-clip sample settings override and the microphone switch. The clips in the scene are listed in
  [content-inventory.md](content-inventory.md).
- **`AudioSampler`**: singleton. `GenerateSpectrumData(AudioSampleSettings)` pulls the FFT bins from
  `AudioSource.GetSpectrumData` and folds them into frequency bands. The band edges are `Constant`, `Direct` (explicit Hz per
  band), `Predefined` (7 bands: 60 / 250 / 500 / 2000 / 4000 / 6000 / 20000 Hz) or `Logarithmic` (each upper bound is
  `MinHertz * ratio^bandIndex`, which matches how pitch is heard). It also weights each band and can normalize with a
  running average, so songs of different loudness come out at a comparable scale.
- **`SpectrumDataContainerModule`**: a thin per-view wrapper around the sampler that recalculates once per frame. It is the
  pipeline's audio entry point. `SamplesContainer` is an earlier, non-module version of the same idea that nothing uses any
  more.
- **`AudioTestSignal`**: replaces the spectrum with a fixed one (key T).

## Views

- **`View`** (`Game/Views/View.cs`): one per scenario, with a `ViewTypeEnum` (`Unity`, `Terrain`, `Dumbbell`), its own root
  object (switched on and off as a whole) and its own `SpectrumDataContainerModule`, so each view samples independently.
- **`ViewManager`**: singleton with the ordered list of views, cycles them with V / Shift+V and restarts the current one
  with R. Only View Terrain is in the list, so V does nothing and the player bar has no view buttons. View Dumbbell, View
  Intro and View Unity are in the scene but switched off and not in the list (see
  [content-inventory.md](content-inventory.md) and [gotchas.md](gotchas.md)). View Intro and View Unity are two separate
  GameObjects that both use `ViewType.Unity`.
- Song selection does not depend on the view: `SimpleControl` (`Core/Scripts/UI/SimpleControl.cs`) owns its own clip list
  and cycles it with Left / Right or the player bar. It also handles pause, volume and the screenshot key.

## Menus

- **Player bar** (bottom): song, pause, volume and camera buttons. Opens when the pointer reaches the bottom edge.
- **`MainMenuUI`**: the small menu next to the player bar with the HDR output toggle, a hotkey overview and Quit. Opens on
  hover or with M / Esc. Only one of it and the terrain menu is open at a time. `QuitConfirmation` asks before the app
  closes (Quit button, Alt+F4 and the window close button).
- **Terrain menu** (right edge, Tab): described below.

## The terrain

Code is in `Assets/Views/View Terrain/Scripts/`.

- **`TerrainGenerator`** builds and updates the mesh every frame. Heights, colors, dampening, waves and mesh morph run as
  Burst jobs on NativeArrays (`TerrainJobs.cs`); the noise is calculated in parallel (`TerrainNoise`, `TerrainNoiseLayer`,
  up to three layers). The band of every vertex per audio shape is cached until a shape or the face count changes;
  triangles and UVs are only rebuilt when needed.
- **`TerrainBeatWave`** detects beats from the bass and the song's BPM and runs the ring waves. **`TerrainLensDirt`** is the
  beat-pulsed lens dirt and also sets the vignette of the volume. **`TerrainRotator`** turns the terrain with the mouse,
  **`TerrainBandPicker`** is the band editor of the test signal.
- **Settings and presets** (`Settings/`): `TerrainSettingsPreset` holds every saved value. Presets and color palettes are
  JSON files in `StreamingAssets/Presets`; builds ship them, new files go to the persistent data path. Old files load with
  defaults for newer fields.
- **Menu UI** (`UI/`): one `TerrainGroupUI...` script per window; the color mapping is in `TerrainColorMapping` and
  `TerrainColorGroupUI`. The shared menu behavior (hover opening, fold, pin, dip and swipe, accent colors, wheel steps)
  lives in `Assets/Core/Scripts/UI/` (`SliderG`, `UiAccent`, `MenuNavigation`, ...).
- **Camera** (`CameraControl`): orbit, move, zoom, keys, beat zoom and sway.

## Render pipeline

HDRP (`com.unity.render-pipelines.high-definition`). The HDR output, bloom, vignette and the tinted sky run through its
volume system. [gotchas.md](gotchas.md) lists the traps from the Unity 6.6 upgrade.
