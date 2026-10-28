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
        public static DirectorTimeline Build()
        {
            DirectorTimeline take = new DirectorTimeline("Hero video")
            {
                // Song = "In Motion",
                EndBeat = 68f
            };

            take.StartBlack()
                .StartMenu(false)
                .StartWith("Video Spacewarp");

            take.Fade(0f, 4f, 0f)                          // fade in from black, one bar
                .Morph(20f, 8f, "Video Ocean")             // Spacewarp glides into Ocean, patterns and mesh detail switch at beat 24
                .Bump("TerrainHeight", 0.2f)               // terrain sinks almost flat while that happens
                .Morph(40f, 8f, "Video Jungle")            // Ocean glides into Jungle, switch at beat 44 (where the cut was)
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
