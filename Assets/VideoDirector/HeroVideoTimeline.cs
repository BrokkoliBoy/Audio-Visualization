namespace Gavi.VideoDirector
{
    /// <summary>
    /// The script of the hero video. Test version: only the three setups in a row (Spacewarp, Ocean, Jungle), no live changes yet.
    /// It uses the copies "Video Spacewarp", "Video Ocean" and "Video Jungle", so the shipped presets stay untouched (see <see cref="DirectorBootstrap.StartPreset"/>).
    /// Beats count from the first beat of the song; at 134 BPM (In Motion) one bar of four beats is 1.8 s, so the take is about 30 s long.
    /// Which presets come when and what happens with the menu open is decided with the shot list (checklist C01).
    /// </summary>
    public static class HeroVideoTimeline
    {
        /// <summary>Third song of the player (DEAFKEV, 100 BPM). The take plays it; the scene starts with it too (<c>SimpleControl._audioStartIndex</c> = 2).</summary>
        public const string SongName = "Invincible";

        /// <summary>The big drop of the song, in beats as the F11 overlay shows them. The transitions are counted from it, in whole bars of 4 beats.</summary>
        public const float DropBeat = 18.5f;

        public static DirectorTimeline Build()
        {
            DirectorTimeline take = new DirectorTimeline("Hero video")
            {
                Song = SongName,
                StartBeat = 0f,                            // the song plays from its first beat; fade in and volume are done in DaVinci Resolve
                EndBeat = 68f
            };

            take.StartMenu(false)
                .StartWith("Video Spacewarp");

            take.Morph(DropBeat + 3f, 6f, "Video Thunderstorm")  // the pattern zoom follows .PullPatternZoom() (needs Noise every = 1 in the presets, otherwise the zoom change ripples); Spacewarp glides into Video Thunderstorm (instead of Video Ocean), starts 3 beats after the drop (beat 21.5), patterns and mesh detail switch at beat 24.5, ends at beat 27.5 (3 beats in, 3 beats out, no shortened second half)
                .ShapeStart(0.825f, 2.3f)                  // the mesh morph (Twist to Planet) starts early like at the second transition (same share of the morph, about 0.9 s after the start) and glides for 2.3 s, until about the end of the morph
                .PullPatternZoom(0.2f, 0.8f)               // the zoom of the patterns moves only a little towards the new zoom first (20 %), strongly around the switch (to 80 %, within about 0.4 s) and then slowly to the end
                .LateGlide("Shapes[0].Zoom")           // the zoom of the audio shape keeps its start value until the switch and only then glides to the zoom of Thunderstorm, so the circle does not shrink and show the undrawn corners in the first half
                .Bump("TerrainHeight", 0.2f)               // terrain sinks almost flat while that happens
                .Bump("MotionWaveDepth", 0f, 0.5f)        // the slow swing (swing size) goes to 0 at the switch and then to the value of Ocean
                .Bump("ColorPatternHue", 0f, 0.5f, false, 0.15f, 3.5f)   // the colors that follow the pattern go out at the switch (they would jump with it):
                .Bump("ColorBandHue", 0f, 0.5f, false, 0.15f, 3.5f)      // slowly down from the start of the morph (3 beats), steep at the switch, back up in about 0.9 beats
                .Bump("ColorPatternGlow", 0f, 0.5f, false, 0.15f, 3.5f)
                .Bump("FloorShare", 0f, 0.5f, false, 0.15f, 3.5f)        // floor glow
                .MorphFromStart(9f, 8f, "Video Jungle")    // starts 9 beats after the start of the Thunderstorm morph (beat 30.5, the number the F11 overlay shows), switch at beat 34.5, ends 38.5
                .StartShape(TerrainShape.AudioShape.Square) // audio shape Circle becomes Square at once when the morph starts (bass stays in the middle, the new corner vertices hide behind the Planet morph), at the switch (beat 34.5) the shape of Jungle follows
                .LateGlidePatternZoom(3f)                  // the pattern zoom stays for the first 3 beats of the 8 (until beat 33.5) and then glides smoothly over the remaining 5 beats (clouds stay recognizable)
                .ShapeStart(1.1f, 3.1f)                  // but the flag turns into the pillar already 0.5 s after the start and glides for 3.1 s (until the end of the morph)
                .Bump("TerrainHeight", 0.2f)
                .Fade(60f, 6f, 1f);                        // fade out to black, so late that the last pattern (Waveform from 56.5) is shown for 3.5 beats first

            // Every pattern of the live part sets its own look; what a line leaves out is the value of Video Jungle (a pattern does not keep the override of the one before).
            // Zoom is the number the slider shows next to the handle (5 = middle of the handle); every pattern has its own range, so the value differs (read from the slider).
            LivePattern(take, 42.5f, TerrainNoise.Pattern.Mosaic, zoom: 0.4081f, depth: 0.85f, layer2Operation: TerrainNoise.LayerOperation.Highest);           // Tiles becomes Mosaic; layer 2 depth stays at Jungle
            LivePattern(take, 44.5f, TerrainNoise.Pattern.Hexagons, zoom: 0.3822f, layer2Operation: TerrainNoise.LayerOperation.Subtract, layer2Depth: 0.4f);      // zoom 6
            LivePattern(take, 46.5f, TerrainNoise.Pattern.Zigzag, zoom: 0.2601f, depth: 0.72f, speedY: 7.3f, layer2Operation: TerrainNoise.LayerOperation.Highest, layer2Depth: 0.4f);
            LivePattern(take, 48.5f, TerrainNoise.Pattern.Dots, zoom: 0.3079f, depth: 0.55f, layer2Operation: TerrainNoise.LayerOperation.Highest, layer2Depth: 0.86f);   // zoom 5.5
            LivePattern(take, 50.5f, TerrainNoise.Pattern.Bricks, zoom: 0.5522f, depth: 0.45f, layer2Operation: TerrainNoise.LayerOperation.Highest, layer2Depth: 0.75f);   // zoom 7.2
            LivePattern(take, 52.5f, TerrainNoise.Pattern.Cracks, zoom: 0.2463f, layer2Operation: TerrainNoise.LayerOperation.Subtract, layer2Depth: 0.4f);
            LivePattern(take, 54.5f, TerrainNoise.Pattern.Ripples, zoom: 0.2745f, depth: 0.4f, speedX: 0f, speedY: 1.6f, layer2Operation: TerrainNoise.LayerOperation.Multiply, layer2Depth: 0.75f);   
            LivePattern(take, 56.5f, TerrainNoise.Pattern.Waveform, zoom: 0.2125f, layer2Operation: TerrainNoise.LayerOperation.Subtract, layer2Depth: 0.4f);
            take.Palette(42.5f, "Thunderstorm")                     // the colors switch too, starting from the palette of Video Jungle
                .Palette(46.5f, "Video Spacewarp")
                .Palette(50.5f, "Lava River");

            take.JumpPoint(38.5f);                                  // end of the Jungle transition, 4 beats before the first live switch: F6 / F7 land here to repeat the live part

            // Examples for the live part (menu open, one thing changes in front of the viewer):
            // take.Menu(24f, true)
            //     .Set(28f, "NoisePattern", 12)                // switch the pattern
            //     .Glide(32f, 8f, "MorphAmount", 0.9f)         // slider glides
            //     .Camera(36f, 8f, 25f, 10f, 0.8f);            // yaw, pitch, distance
            return take;
        }

        // Values of Video Jungle, to which a pattern of the live part returns when it sets nothing of its own.
        private const float JungleZoom = 0.42724f;
        private const float JungleDepth = 0.475f;
        private const float JungleSpeedX = -1.4224f;
        private const float JungleSpeedY = -1.13334f;
        private const TerrainNoise.LayerOperation JungleLayer2Operation = TerrainNoise.LayerOperation.Subtract;
        private const float JungleLayer2Depth = 0.7f;

        /// <summary>Switches the main pattern and sets zoom, depth, scroll speed X and Y and operation and depth of layer 2 (Domain Warp) on the same beat; what is left out falls back to Video Jungle.</summary>
        private static void LivePattern(DirectorTimeline take, float beat, TerrainNoise.Pattern pattern, float zoom = JungleZoom, float depth = JungleDepth, float speedX = JungleSpeedX, float speedY = JungleSpeedY,
            TerrainNoise.LayerOperation layer2Operation = JungleLayer2Operation, float layer2Depth = JungleLayer2Depth)
        {
            take.Pattern(beat, pattern)
                .Set(beat, "PerlinZoom", zoom)
                .Set(beat, "PerlinDepth", depth)
                .Set(beat, "PerlinSpeedX", speedX)
                .Set(beat, "PerlinSpeedY", speedY)
                .Set(beat, "NoiseLayer2.Operation", (int)layer2Operation)
                .Set(beat, "NoiseLayer2.Depth", layer2Depth);
        }
    }
}
