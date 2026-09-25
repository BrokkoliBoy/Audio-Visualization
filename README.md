# Audio Visualization

An audio-reactive visual effects sandbox built around a small, hand-rolled node-based "pipeline" for
routing live audio data into object properties — no code required per new effect.

## Why I built this

This started as a side quest: I wanted a wall in a different project to move along with whatever
song was playing. I quickly realized the same idea could drive all kinds of visuals, so I split it
off into its own dedicated project and just had fun seeing what I could build with it. It became a
nice outlet for creativity beyond "does the gameplay work."

## Concept

The core idea: pull a channel's float value out of Unity's audio spectrum sampler (e.g. the bass
band), run it through a chain of small transform scripts (add, multiply, clamp, lerp, remap, ...),
convert the type if needed (into a color, an animation curve, whatever the target expects), and hand
the result to an "applicator" script that pushes it onto a real component property — a light's
radius, an object's scale, a particle system's emission rate, a camera's field of view.

Every piece of that chain is a small, reusable `MonoBehaviour` you wire together by dragging
components onto GameObjects — something like a lightweight, code-free visual-scripting system built
specifically for "audio in, property out."

## Under the hood

- **A three-stage module pipeline** — Source (pull data out of the audio system), Transform (math
  operations, type conversions), Apply ("Modifiables" that push the final value onto a real Unity
  component property) — with a consistent naming convention (`VoidToFloatModule`,
  `FloatToFloatModule`, ...) that makes the type flow of any chain readable at a glance.
- **Configurable spectrum sampling**: FFT bins folded into frequency bands using one of several
  distribution strategies (constant, explicit per-band Hz values, a predefined 7-band table, or
  logarithmic — matching how humans actually perceive pitch), plus optional running-average
  normalization so tracks with very different loudness still visualize at a comparable scale.
- **Beat-synced effects**: each song carries its own BPM, beat offset, and an animation-curve
  "beatmap" describing how a beat should hit and decay, driving effects like the camera's zoom pulse.
- **Multiple independent views**, each with its own audio sampler instance, switchable at runtime
  without unloading the scene.

## Credits

Four songs used in this project are free-to-use releases from [NCS (NoCopyrightSounds)](https://ncs.io/):

- Song: DEAF KEV - Invincible [NCS Release] · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/invincible · Watch: http://youtu.be/J2X5mJ3HDYE
- Song: Lynxie, johnny joined - i still hear your voice · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/istillhearyourvoice · Watch: http://ncs.lnk.to/istillhearyourvoiceAT/youtube
- Song: L3VI - in motion · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/inmotion · Watch: https://ncs.lnk.to/inmotionAT/youtube
- Song: SILVER BULLET - Nobody Else · Music provided by NoCopyrightSounds · Free Download/Stream: http://ncs.io/S_NobodyElse · Watch: https://ncs.lnk.to/S_NobodyElseAT/youtube
