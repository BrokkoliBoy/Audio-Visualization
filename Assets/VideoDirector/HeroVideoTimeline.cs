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
        /// <summary>Third song of the player (DEAFKEV, 100 BPM). The app starts with it on this branch (<see cref="DirectorSong"/>), the take plays it.</summary>
        public const string SongName = "Invincible";

        public static DirectorTimeline Build()
        {
            DirectorTimeline take = new DirectorTimeline("Hero video")
            {
                Song = SongName,
                StartBeat = 8f,                            // the song starts here: the slow first 8 beats are left out, the drop (beat 18.5) comes 10.5 beats into the take
                EndBeat = 68f
            };

            take.StartBlack()
                .StartMute()
                .StartMenu(false)
                .StartWith("Video Spacewarp");

            take.Fade(8f, 4f, 0f)                          // fade in from black, one bar
                .Volume(8f, 8f, 1f, Ease.In)               // the music comes up from mute to full, slowly at first, and is at full volume shortly before the drop
                .Morph(20f, 8f, "Video Ocean")             // Spacewarp glides into Ocean, patterns and mesh detail switch at beat 24
                .Bump("TerrainHeight", 0.2f)               // terrain sinks almost flat while that happens
                .Bump("MorphAmount", 0f, 0.5f)             // the twist is fully unwound at the switch, then the flag grows from 0 to Ocean's amount
                .Bump("MotionWaveDepth", 0f, 0.5f)        // the slow swing (swing size) goes to 0 at the switch and then to the value of Ocean
                .Bump("ColorPatternHue", 0f, 0.5f, false, 0.15f, 3.5f)   // the colors that follow the pattern go out at the switch (they would jump with it):
                .Bump("ColorBandHue", 0f, 0.5f, false, 0.15f, 3.5f)      // slowly down from the start of the morph (4 beats), steep at the switch, back up in 1.2 beats
                .Bump("ColorPatternGlow", 0f, 0.5f, false, 0.15f, 3.5f)
                .Bump("FloorShare", 0f, 0.5f, false, 0.15f, 3.5f)        // floor glow
                .Bump("Shapes[0].Zoom", 0.68f, 0.3f, true)        // the circle of Spacewarp opens up to the whole square, so the corners are not drawn in one frame at the switch
                .Morph(40f, 8f, "Video Jungle")            // Ocean glides into Jungle, switch at beat 44 (where the cut was)
                .ShapeStart(1.1f, 3.1f)                    // but the flag turns into the pillar already 0.5 s after the start and glides for 3.1 s (until the end of the morph)
                .Bump("TerrainHeight", 0.2f)
                .Fade(60f, 6f, 1f);                        // fade out to black

            // Examples for the live part (menu open, one thing changes in front of the viewer):
            // take.Menu(24f, true)
            //     .Set(28f, "NoisePattern", 12)                // switch the pattern
            //     .Glide(32f, 8f, "MorphAmount", 0.9f)         // slider glides
            //     .Camera(36f, 8f, 25f, 10f, 0.8f);            // yaw, pitch, distance
            return take;
        }
    }
}
