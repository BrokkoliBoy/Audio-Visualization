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
                .Morph(DropBeat + 4f, 6f, "Video Ocean")   // Spacewarp glides into Ocean, starts 4 beats after the drop (beat 22.5), patterns and mesh detail switch at beat 25.5
                .AfterSwitchTime(0.5f)                     // the part after the switch takes half its time (3 beats to 1.5): the first part is twice as long as the second, the morph is 4.5 beats (2.7 s) long and ends at beat 27
                .Bump("TerrainHeight", 0.2f)               // terrain sinks almost flat while that happens
                .Bump("MorphAmount", 0f, 0.5f)             // the twist is fully unwound at the switch, then the flag grows from 0 to Ocean's amount
                .Bump("MotionWaveDepth", 0f, 0.5f)        // the slow swing (swing size) goes to 0 at the switch and then to the value of Ocean
                .Bump("ColorPatternHue", 0f, 0.5f, false, 0.15f, 3.5f)   // the colors that follow the pattern go out at the switch (they would jump with it):
                .Bump("ColorBandHue", 0f, 0.5f, false, 0.15f, 3.5f)      // slowly down from the start of the morph (3 beats), steep at the switch, back up in about 0.45 beats
                .Bump("ColorPatternGlow", 0f, 0.5f, false, 0.15f, 3.5f)
                .Bump("FloorShare", 0f, 0.5f, false, 0.15f, 3.5f)        // floor glow
                .Bump("Shapes[0].Zoom", 0.68f, 0.3f, true)        // the circle of Spacewarp opens up to the whole square, so the corners are not drawn in one frame at the switch
                .MorphFromStart(7f, 8f, "Video Jungle")    // starts 7 beats after the start of the Ocean morph (beat 29.5, the number the F11 overlay shows), switch at beat 33.5, ends 37.5
                .ShapeStart(1.1f, 3.1f)                    // but the flag turns into the pillar already 0.5 s after the start and glides for 3.1 s (until the end of the morph)
                .Bump("TerrainHeight", 0.2f)
                .Fade(52f, 6f, 1f);                        // fade out to black

            // Live part after the Jungle transition (ends at beat 37.5): hard cuts on the beat, one change every 2 beats. The rest follows.
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
