# Gotchas

Non-obvious traps discovered while working on this codebase. Read this before assuming a bug is
where it looks like it is.

## `com.unity.ugui` doesn't survive the automatic Unity 6.6 upgrade

Same trap already documented in PocketHealer and ConwaysGameOfLife: upgrading past the Unity
version where `UnityEngine.UI` was split out of the built-in modules into the standalone
`com.unity.ugui` package does not automatically add that package to `Packages/manifest.json`,
breaking every `UnityEngine.UI` reference project-wide. Fixed 2026-09-23 by adding
`"com.unity.ugui": "2.6.0"` to the manifest by hand (version taken from the installed 6000.6.2f1
Editor's own bundled default manifest). A running Editor does not notice the external
`manifest.json` edit until it regains focus (alt-tab back into it, or restart).

## `EntityId` migration is two-staged, not one drop-in swap

Vendored third-party code (`Assets/Plugins/AssetUsageDetector`) needed hand-patching for Unity
6.6's instance-ID-to-EntityId migration, same category of fix as PocketHealer's Doozy patches, but
with an extra twist worth remembering for the *next* Unity upgrade too:

1. First round: `Object.GetInstanceID()`, `EditorUtility.InstanceIDToObject(int)`,
   `AssetDatabase.Contains(int)` are all obsolete-as-error in 6.6. Renaming to
   `Object.GetEntityId()`, `EditorUtility.EntityIdToObject(EntityId)`,
   `AssetDatabase.Contains(EntityId)` looks like the fix, and passing a plain `int` compiles fine
   at this point because of an implicit `int -> EntityId` conversion operator.
2. Second round: that implicit (and the equivalent explicit) `int -> EntityId` conversion operator
   is **itself** obsolete-as-error ("EntityId will not be representable by an int in the future.
   This casting operator will be removed in a future version.") - so step 1's fix still doesn't
   compile once you touch anything that needs an actual `EntityId` constructed from a stored `int`.

The real fix: never convert `int <-> EntityId` through a cast at all. Use the static methods
instead: `EntityId.ToULong(entityId)` to get a plain `int`-storable value out of an `EntityId` (for
serialization or plain-int fields you don't want to change the type of), and
`EntityId.FromULong((ulong) storedInt)` to reconstruct the `EntityId` back for API calls that need
one. Applied in `SearchResult.cs` (`ReferenceNode`/`ReferenceNodeGUI`'s `int?` fields were kept as
`int?` rather than retyped to `EntityId?`, since one of them round-trips through a `[Serializable]`
`SerializableNode.instanceId : int` used for domain-reload persistence) and `Utilities.cs`
(`previousPingedPrefabInstanceId`, a plain `int` used for a double-click timer, doesn't need
`EntityId` at all, just `EntityId.ToULong(obj.GetEntityId())` to compare against it).

If a *future* Unity upgrade surfaces more EntityId-related errors, check whether the specific
operator/method used is itself now flagged obsolete before assuming the previous fix pattern still
applies - this is exactly what happened here.

## HDRP material migration after a render pipeline package bump

Separate from the compile errors above: after the HDRP package itself jumped versions (bundled
with the Unity 6.6 upgrade), the HDRP Wizard's "Project Migration Quick Links" flagged materials
using shaders it has no automated upgrader for. Two independent things happened here, don't conflate
them:

- **Materials still on a Built-in-render-pipeline shader** (Standard, Legacy Shaders, ...) can be
  auto-converted to an HDRP-equivalent shader via "Convert all built-in materials to HDRP". Applied
  2026-09-23.
- **Materials already on an HDRP shader** (`HDRP/Lit` etc.) can still carry stale internal data
  from an older HDRP package version (keywords, serialized property layout) even though they still
  "compile"/render without an explicit error. "Upgrade HDRP materials to latest version" re-syncs
  them to the currently installed HDRP package's expected data layout. Applied 2026-09-23.
- Materials on a fully custom/unrecognized shader are skipped by both of the above with "no
  material upgrader defined" - that's expected, not a sign anything went wrong. They need manual
  attention if they're actually broken (see the two entries below).

## `Circle.mat`'s broken shader predates all of the above, and is unrelated to HDRP

`Assets/Assets/Materials/Beat circles/Circle.mat` shows `Hidden/InternalErrorShader` in the
Editor. This is **not** an HDRP compatibility issue: its `m_Shader` GUID
(`0406db5a14f94604a8c57ccfbc9f3b46`) does not resolve to any file anywhere in the project (`Assets/`
or `Packages/`) - the shader asset itself is simply missing, independent of render pipeline or
Unity version. Confirmed low priority by the dev (2026-09-23), not investigated further.

## View Dumbbell has been broken since an earlier, incomplete SRP upgrade

Per the dev (2026-09-23): View Dumbbell's reflections/lighting broke during an earlier Unity
version upgrade that moved the project onto HDRP; the view's own materials/lighting setup was only
ever made for the render pipeline at the time (not adjusted for HDRP), and it was never revisited.
Not something today's Unity 6.6 upgrade caused or fixed. Current plan is to present only View
Terrain and the Unity view (see architecture.md's "Naming trap" for why "View Intro" and "the Unity
view" are the same object), and only look at View Dumbbell again if there's time left over.

## Songs are real, copyrighted, commercially released tracks, committed straight into git

`Assets/Assets/Audio/Clips/*.mp3` are full audio files of actual released songs (GRiZ, Jamie
Berry ft. Rosie Harte, a Stranger Things theme remix, a Jujutsu Kaisen ending theme, two Zelda
piano arrangements, a radio ad jingle, ...), not royalty-free placeholder audio, and they are
tracked in git (not gitignored) in a repo whose `origin` remote is a public GitHub repo
(`BrokkoliBoy/AudioVisualization`). See content-inventory.md for the full list and gotchas this
raises for showing this repo publicly.
