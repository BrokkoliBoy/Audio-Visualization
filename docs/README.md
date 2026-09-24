# AudioVisualization - Codebase Docs (for AI sessions)

This `docs/` folder exists so a fresh Claude Code session (or any other AI
session) working on this repo doesn't have to re-read every script, scene,
and prefab from scratch to get oriented. Read this before exploring the
codebase yourself.

- [architecture.md](architecture.md) - the pipeline system (audio sampling, math modules, modifiables), views, what lives where
- [gotchas.md](gotchas.md) - non-obvious traps and quirks discovered the hard way
- [changelog.md](changelog.md) - dated log of AI-assisted sessions and what they changed, so you don't rediscover or redo the same thing
- [content-inventory.md](content-inventory.md) - living list of every song and every view (stats, status, known gaps) - keep this one updated whenever content changes

## Quick facts

- Unity 6000.6.2f1 (upgraded from 2022.3.62f3 on 2026-09-23).
- Render pipeline: HDRP (`com.unity.render-pipelines.high-definition`, 17.6.0).
- Namespace `Gavi` throughout (same convention as PocketHealer), with the actual pipeline system under `Gavi.Modules.*`, `Gavi.Audio`, `Gavi.Modifiables`.
- The `com.unity.pipeline` package is installed, so a live Unity Editor for this project can be driven directly via the `unity` CLI (`unity status`, `unity command`, etc.) instead of hand-editing scene/prefab YAML - check `unity status` first in any session that needs to touch a scene or GameObject.
- Songs live as real audio files on disk at `Assets/Assets/Audio/Clips/*.mp3`, committed to git. Several are commercially released, copyrighted tracks, not royalty-free or Chris's own work - see content-inventory.md and gotchas.md before assuming it's fine to show/share this repo as-is.
- Controls: Tab / Shift+Tab cycles views, R restarts the current view (replays the audio from 0), F1 / F2 cycles songs, P pauses/unpauses, +/- adjusts volume.
