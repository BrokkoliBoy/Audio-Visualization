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
                StartBeat = 4f,                            // the song starts here: the slow first 4 beats are left out, the drop (DropBeat) comes 14.5 beats into the take
                EndBeat = 60f
            };

            take.StartBlack()
                .StartMute()
                .StartMenu(false)
                .StartWith("Video Spacewarp");

            take.Fade(4f, 4f, 0f)                          // fade in from black, one bar
                .Volume(4f, 4f, 1f, Ease.In)               // the music comes up from mute to full within 4 beats, slowly at first
                .Morph(DropBeat + 3f, 6f, "Video Thunderstorm")  // the pattern zoom follows .PullPatternZoom() (needs Noise every = 1 in the presets, otherwise the zoom change ripples); Spacewarp glides into Video Thunderstorm (instead of Video Ocean), starts 3 beats after the drop (beat 21.5), patterns and mesh detail switch at beat 24.5, ends at beat 27.5 (3 beats in, 3 beats out, no shortened second half)
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
                .Fade(52f, 6f, 1f);                        // fade out to black

            // Live part after the Jungle transition (ends at beat 38.5): hard cuts on the beat, one change every 2 beats. The rest follows.
            take.Pattern(42.5f, TerrainNoise.Pattern.Checker)       // Tiles becomes Checker
                .Pattern(44.5f, TerrainNoise.Pattern.Hexagons)
                .Pattern(46.5f, TerrainNoise.Pattern.Ripples)
                .Pattern(48.5f, TerrainNoise.Pattern.Zigzag)        // still open: Dots, Bricks
                .Palette(46.5f, "Thunderstorm")                     // towards the end the colors switch too, starting from the palette of Video Jungle
                .Palette(48.5f, "Lava River")
                .Palette(50.5f, "Video Spacewarp");

            // Examples for the live part (menu open, one thing changes in front of the viewer):
            // take.Menu(24f, true)
            //     .Set(28f, "NoisePattern", 12)                // switch the pattern
            //     .Glide(32f, 8f, "MorphAmount", 0.9f)         // slider glides
            //     .Camera(36f, 8f, 25f, 10f, 0.8f);            // yaw, pitch, distance
            return take;
        }
    }
}
