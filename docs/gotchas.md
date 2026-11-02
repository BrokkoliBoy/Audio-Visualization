# Gotchas

Non-obvious traps in this project. Read this before assuming a bug is where it looks like it is.

## `com.unity.ugui` is not added by the Unity 6.6 upgrade

`UnityEngine.UI` was split out of Unity's built-in modules into the package `com.unity.ugui`, and the automatic upgrade does
not add it to `Packages/manifest.json`. Without it every `UnityEngine.UI` reference fails. The project has it by hand
(`"com.unity.ugui": "2.6.0"`, the version from the Editor's own default manifest). A running Editor only notices an
external edit of `manifest.json` when it regains focus.

## `EntityId` migration of the AssetUsageDetector plugin

The vendored plugin `Assets/Plugins/AssetUsageDetector` needed hand-patching for Unity 6.6's move from instance IDs to
`EntityId`. Renaming `GetInstanceID()` to `GetEntityId()` is not enough: the `int <-> EntityId` cast is itself obsolete.
Convert with `EntityId.ToULong(id)` and `EntityId.FromULong((ulong) value)` instead, as `SearchResult.cs` and `Utilities.cs`
do. If a later Unity upgrade brings more `EntityId` errors, check first whether the method you used is flagged obsolete too.

## HDRP material migration after a package update

After the HDRP package jumped versions, the HDRP Wizard flagged materials. Two different things were done, and they are not
the same: "Convert all built-in materials to HDRP" for materials still on a Built-in shader, and "Upgrade HDRP materials to
latest version" for materials already on an HDRP shader whose data layout was out of date. Materials on a custom shader are
skipped by both with "no material upgrader defined", which is expected.

## `Circle.mat` shows the error shader

`Assets/Assets/Materials/Beat circles/Circle.mat` shows `Hidden/InternalErrorShader`. Its shader GUID
(`0406db5a14f94604a8c57ccfbc9f3b46`) does not resolve to any file in the project, so the shader asset is simply missing. This
is not an HDRP problem, and no scene or prefab uses the material.

## View Dumbbell is broken for HDRP

Its reflections and lighting broke when the project moved to HDRP and were never redone. It is not in the `ViewManager`
list, but it stays in the scene for later.

## `ViewManager.Start()` does not deactivate a view that was saved active

`SetViewActive` only stops the previous view when `_currentIndex >= 0`, which it is not yet in `Start()`. A view saved with
`activeSelf = true` therefore stays on next to the one `ViewManager` activates, which shows up as two `AudioListener`s. All
views are saved inactive; if two views appear, check each view's `activeSelf` in the scene before blaming `ViewManager`.
