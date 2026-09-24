# Changelog (AI-assisted sessions)

Dated log of what an AI session changed and why, so a future session doesn't rediscover or redo the
same work. Append to this, don't rewrite history.

## 2026-09-23 - Unity 6.6 upgrade fallout, HDRP material migration, docs folder created

- Unity upgraded 2022.3 -> 6000.6.2f1 (done by the dev before this session started).
- Installed `com.unity.pipeline` (`unity pipeline install`) so a live Editor can be driven via the
  `unity` CLI instead of hand-editing scene/prefab YAML, matching PocketHealer's setup.
- Fixed the missing `com.unity.ugui` package (added `"com.unity.ugui": "2.6.0"` to
  `Packages/manifest.json`) - this alone was the original console error, a `CS0234` on
  `UnityEngine.UI` inside the vendored `AssetUsageDetector` plugin.
- Fixed 6 further compile errors in the same plugin (`SearchResult.cs`, `Utilities.cs`) from
  Unity 6.6's EntityId API migration (`GetInstanceID`/`InstanceIDToObject`/`AssetDatabase.Contains(int)`
  all obsolete-as-error), including a second round of fixes after discovering the initial
  `int -> EntityId` cast-based fix was itself obsolete - see gotchas.md for the exact pattern
  (`EntityId.FromULong`/`EntityId.ToULong`, no casts). Verified via the Editor.log: `Tundra build
  success`, zero `error CS` remaining.
- Ran the HDRP Wizard's material migration ("Convert all built-in materials to HDRP" and "Upgrade
  HDRP materials to latest version", both run by the dev). Confirmed via a live Editor `eval` query
  across every `Material` asset in the project which ones use which shader, both before and to
  identify what would be skipped - see gotchas.md for what each button actually does and why both
  were needed (two independent problems: never-HDRP materials, and stale-HDRP-version materials).
- Investigated `Circle.mat`'s broken shader (shown as `Hidden/InternalErrorShader`): confirmed via
  its raw YAML that the referenced shader GUID doesn't resolve to any file in the project at all,
  i.e. this is a missing-asset problem unrelated to the render pipeline or today's upgrade. Not
  fixed, dev said it's low priority for now.
- Captured context from the dev on View Dumbbell being broken (reflections/lighting, from an
  earlier incomplete SRP-to-HDRP upgrade, unrelated to today) and on the intended presentation set
  (View Terrain + the Unity view only, View Dumbbell only if time allows).
- Created this `docs/` folder (README.md, architecture.md, gotchas.md, changelog.md,
  content-inventory.md), mirroring PocketHealer's structure. Pulled the pipeline architecture from
  reading the actual `Module`/`Modifiable`/`Audio` scripts, and the song/view inventory live from
  the open Editor via `unity cmd eval` (every `AudioClipContainer` in the scene, and
  `ViewManager`'s configured views list read via reflection in Edit mode).
- Found while building the song inventory: the project's audio clips
  (`Assets/Assets/Audio/Clips/*.mp3`) are real, commercially released/licensed tracks (GRiZ, Jamie
  Berry, a Stranger Things remix, a Jujutsu Kaisen ending theme, two Zelda piano arrangements, a
  radio ad jingle, ...), committed straight into git, in a repo whose GitHub remote is public. Not
  a code bug, but flagged as an open portfolio-readiness question - see content-inventory.md.
