# Audio Visualization

**Status:** Finished  |  **Built:** 2017 to 2019  |  **Role:** Solo developer

An audio-reactive visual effects sandbox I built on the side during my studies, around a small,
hand-rolled node-based "pipeline" for routing live audio data into object properties. No code
required per new effect.

## Why I built this

This started as a side quest: I wanted a wall in a different project to move along with whatever
song was playing. I quickly realized the same idea could drive all kinds of visuals, so I split it
off into its own dedicated project and just had fun seeing what I could build with it. It became a
nice outlet for creativity beyond "does the gameplay work."

## Concept

The core idea: pull a channel's float value out of Unity's audio spectrum sampler (e.g. the bass
band), run it through a chain of small transform scripts (add, multiply, clamp, lerp, remap, ...),
convert the type if needed (into a color, an animation curve, whatever the target expects), and hand
the result to an "applicator" script that pushes it onto a real component property, for example a light's
radius, an object's scale, a particle system's emission rate or a camera's field of view.

Every piece of that chain is a small, reusable `MonoBehaviour` you wire together by dragging
components onto GameObjects. It works a bit like a lightweight visual scripting system built
specifically for "audio in, property out."

## Features

Two switchable visual scenarios ("views"), each with its own audio sampler. Only one of them was built with the pipeline system; the other is the very first thing I made in this project, from before the pipeline existed. Tab / Shift+Tab cycles through the views, R restarts the current one.

Four free-to-use songs plus a 20 to 20,000 Hz test sweep and live microphone input, cycled with F1 / F2.

Beat-synced camera zoom, driven by each song's BPM and an animation-curve "beatmap".

Effects built purely by chaining modules in the Inspector: lights, scale, position, rotation, materials, VFX Graph particles, and camera field of view.

## Under the hood

**A three-stage module pipeline.** Source (pull data out of the audio system), Transform (math operations, type conversions), Apply ("Modifiables" that push the final value onto a real Unity component property), with a consistent naming convention (`VoidToFloatModule`, `FloatToFloatModule`, ...) that makes the type flow of any chain readable at a glance.

**Configurable spectrum sampling.** FFT bins folded into frequency bands using one of several distribution strategies (constant, explicit per-band Hz values, a predefined 7-band table, or logarithmic, which matches how humans perceive pitch), plus optional running-average normalization so tracks with very different loudness still visualize at a comparable scale.

**Beat-synced effects.** Each song carries its own BPM, beat offset, and an animation-curve "beatmap" describing how a beat should hit and decay.

**Independent views.** Each view has its own audio sampler instance and can be switched at runtime without unloading the scene.

## Where it stands

Finished as a sandbox. The dumbbell scene is still in the project but switched off, because its lighting predates the move to HDRP and was never reworked. If I pick this back up, I'd rework that view for HDRP and put it back into the rotation, add more views and effect chains, and clean up the data flow in the module pipeline, which currently only runs one way.

## Credits

Four songs used in this project are free-to-use releases from [NCS (NoCopyrightSounds)](https://ncs.io/):

- Song: DEAF KEV - Invincible [NCS Release] · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/invincible · Watch: http://youtu.be/J2X5mJ3HDYE
- Song: Lynxie, johnny joined - i still hear your voice · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/istillhearyourvoice · Watch: http://ncs.lnk.to/istillhearyourvoiceAT/youtube
- Song: L3VI - in motion · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/inmotion · Watch: https://ncs.lnk.to/inmotionAT/youtube
- Song: SILVER BULLET - Nobody Else · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/S_NobodyElse · Watch: https://ncs.lnk.to/S_NobodyElseAT/youtube
