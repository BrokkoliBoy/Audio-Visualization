# Architecture

An audio-reactive visual effects sandbox from ~2017-2019. Its actual point is not any one visual
effect, it's the pipeline system underneath: a lightweight, hand-rolled stand-in for visual
scripting that lets you wire together "pull a float out of the audio spectrum, transform it, apply
it to a component property" chains entirely by dragging components onto GameObjects, without
writing new code for each new effect.

Namespace is `Gavi` throughout (same convention as PocketHealer), split into `Gavi.Audio`,
`Gavi.Modules.*`, `Gavi.Modifiables`, `Gavi` (Views), `Gavi.Utility`, `Gavi.Events`.

## The pipeline: source -> transform -> apply

Every node in the pipeline is a `Module` (`Assets/Core/Scripts/Game/Modules/Module.cs`), a
`MonoBehaviour` base class that deliberately does **not** implement `Update`/`FixedUpdate`/
`LateUpdate` itself (they're commented out in the base class with a note that they must be
declared in the leaf class instead, so a `Module` that doesn't need per-frame work doesn't pay for
an empty `Update()` call). A `Module` also renames its own GameObject to a descriptive name
(`GetName()`, override per subclass) via `OnDrawGizmosSelected`, so a pipeline graph in the
Hierarchy is self-labeling instead of a wall of "GameObject (1)".

Three families of `Module` subclasses, matching the three pipeline stages:

1. **Source ("Audio Modules")** - `Assets/Core/Scripts/Game/Modules/Audio Modules/`. These pull
   data out of the audio system with no upstream `Module` input, hence the naming convention
   `VoidToXModule` (e.g. `SpectrumDataContainerModule : VoidToFloatArrayModule`). This is the only
   place the pipeline touches `Gavi.Audio` directly.
2. **Transform ("Math Modules")** - `Assets/Core/Scripts/Game/Modules/Math Modules/`, organized
   first by output type (`Float`, `Int`, `Color`), then by conversion shape as a naming convention:
   `FloatToFloatModules` (clamp, decay, lerp, remap, add, subtract, multiply, modulo, store, ...),
   `FloatArrayToFloatModules`, `FloatArrayToFloatCollectionModules`, `VectorModules`,
   `FunctionModules`, etc. Each module references the upstream module(s) it reads from via a
   serialized field, so a chain is just a sequence of sibling/child GameObjects each pointing at
   the previous one.
3. **Apply ("Modifiables")** - `Assets/Core/Scripts/Game/Modifiables/`. Plain `MonoBehaviour`s
   (not `Module` subclasses), each specialized for one kind of target: `MaterialPropertyModifiable`
   (float/color shader properties via `Material.SetFloat`/`SetColor`), `LightModifiable`,
   `CameraModifiable`, `PositionModifiable`, `RotationModifiable`, `ScaleModifiable`,
   `VFXGraphModifiable` (VFX Graph exposed properties), `OrganicMovementModifiable`. Each holds a
   list of `(propertyId, Module)` pairs and calls `Module.GetValue()` every `Update()` to push the
   current value onto the real Unity property.

This is the "supply chain" described in the top-level README's Concept section: pull a channel's
float value from the audio sampler, run it through however many math modules, convert type if
needed, and hand it to a Modifiable that actually changes something visible.

## Audio: sampling and playback

- **`AudioPlayer`** (`Assets/Core/Scripts/Game/Audio/AudioPlayer.cs`) - singleton, owns the single
  `AudioSource` that actually plays. `Play(AudioClipContainer)` swaps between a music mixer group
  and a microphone mixer group depending on `clip.UseMic`; microphone mode starts a
  `Microphone.Start` loop clip instead of assigning a real `AudioClip`.
- **`AudioClipContainer`** (`Assets/Core/Scripts/Game/Audio/AudioClipContainer.cs`) - wraps one
  song: the `AudioClip` itself, BPM, a beat offset, an optional `FunctionModule` beatmap (shapes how
  strong/fast the beat hits), an optional per-clip `AudioSampleSettingsPrefab` override, and the mic
  toggle. See content-inventory.md for every clip currently wired into the scene.
- **`AudioSampler`** (`Assets/Core/Scripts/Game/Audio/AudioSampler.cs`) - singleton,
  `GenerateSpectrumData(AudioSampleSettings)` pulls raw FFT bins via
  `AudioSource.GetSpectrumData` and folds them into a configurable number of frequency bands.
  Band-boundary distribution is one of `Constant`, `Direct` (explicit per-band Hz values),
  `Predefined` (a hardcoded 7-band table: 60/250/500/2000/4000/6000/20000 Hz), or `Logarithmic`
  (default-feeling one: each band's upper bound is `MinHertz * ratio^bandIndex`, so early bands
  cover less spectrum and later bands cover more, matching how humans perceive pitch/frequency).
  Also handles amplitude weighting per band and an optional running-average normalization
  (`UseAveraging`) so tracks with wildly different loudness still visualize at a comparable scale.
- **`SamplesContainer`** / **`SpectrumDataContainerModule`** - both are thin per-consumer wrappers
  around `AudioSampler`, recalculating lazily (once per frame, guarded by a dirty flag) rather than
  on every property access. `SpectrumDataContainerModule` is the actual `Module` subclass used as
  the pipeline's audio entry point; `SamplesContainer` looks like an earlier, non-`Module` version
  of the same idea that's no longer used by the pipeline itself.

## Views

- **`View`** (`Assets/Core/Scripts/Game/Views/View.cs`) - one per scenario. Holds a `ViewTypeEnum`
  (`Unity = 0`, `Terrain = 1`, `Dumbbell = 2`), a reference to its own root `_viewObject`
  (SetActive toggles the whole scenario on/off), and its own `SpectrumDataContainerModule` (each
  view samples audio independently rather than sharing one global sampler instance).
- **`ViewManager`** (`Assets/Core/Scripts/Game/Views/ViewManager.cs`) - singleton, holds the
  ordered `List<View>`, cycles through them on V / Shift+V, restarts the current one (replays
  the audio from 0) on R. Currently configured with 2 entries (View Terrain, View Intro) - View
  Dumbbell exists in the scene but isn't in this list, so it's excluded from the rotation without
  being deleted. See content-inventory.md and gotchas.md.
- **Naming note**: "View Intro" and "View Unity" are two separate GameObjects that both happen to
  use `ViewType.Unity` - despite the similar naming, they are not the same object. Only "View Intro"
  is wired into `ViewManager`; "View Unity" is an unused, inactive duplicate left in the scene.
- Song selection is independent of the view: `SimpleControl`
  (`Assets/Core/Scripts/UI/SimpleControl.cs`) owns its own separate `List<AudioClipContainer>` and
  cycles it on Left / Right arrow (or the menu buttons), so any song can play under any view.

## Menu

`MainMenuUI` (`Assets/Core/Scripts/UI/MainMenuUI.cs`) is a slide-in uGUI panel (bottom right) that
mirrors every hotkey as a button: song and view switching, restart, pause, a volume slider, and
quit. M and Esc toggle it. It calls public methods on `SimpleControl` (`NextAudio`, `PreviousAudio`,
`TogglePause`, `SetVolume`) and `ViewManager` (`NextView`, `PreviousView`, `RestartView`), so the
buttons and the hotkeys share one code path. `Quit` calls `Application.Quit()` only. The old
Esc help panel in `BasicUi` was removed. WebGL is not supported (HDRP).

## Render pipeline

HDRP (`com.unity.render-pipelines.high-definition`). See gotchas.md for the Unity 6.6 upgrade
fallout (missing `com.unity.ugui`, the `AssetUsageDetector` plugin's EntityId migration, and the
HDRP material migration wizard) and content-inventory.md for per-view render state (View Dumbbell
is currently broken here, unrelated to this session's fixes).
