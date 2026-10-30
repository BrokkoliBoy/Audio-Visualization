namespace Gavi.VideoDirector
{
    /// <summary>
    /// The script of the intro video for the GitHub README (about 15 s, hard cut at the end). It uses only the copies "Intro Thunderstorm Live" and "Intro Jungle Live"
    /// with their palettes, never the presets of the hero video or of next. Song and beats as in the hero video (Invincible, 100 BPM, 1 beat = 0.6 s).
    /// The details (start beat, patterns, spacing) are a first version and meant to be tuned.
    /// </summary>
    public static class IntroVideoTimeline
    {
        public const float StartBeat = 14.5f;
        public const float DropBeat = 18.5f;

        public static DirectorTimeline Build()
        {
            DirectorTimeline take = new DirectorTimeline("Intro video")
            {
                Song = HeroVideoTimeline.SongName,
                StartBeat = StartBeat,
                EndBeat = 39f                              // hard cut 1.5 s after the end of the morph, fade and volume are done in DaVinci Resolve
            };

            take.StartMenu(true)                           // the menu is open from the first picture, so the viewer sees what can be changed
                .StartWith("Intro Thunderstorm Live");

            // Four changes in front of the viewer, one every 2 beats after the drop. The zoom is the number the slider shows, every pattern has its own range.
            IntroPattern(take, DropBeat + 2f, TerrainNoise.Pattern.Hexagons, 0.3822f);
            IntroPattern(take, DropBeat + 4f, TerrainNoise.Pattern.Zigzag, 0.2601f);
            IntroPattern(take, DropBeat + 6f, TerrainNoise.Pattern.Dots, 0.3079f);
            IntroPattern(take, DropBeat + 8f, TerrainNoise.Pattern.Bricks, 0.5522f);

            take.Menu(27.5f, false);                       // the menu closes before the morph

            // Same recipe as the Thunderstorm to Jungle morph of the hero video (two places to maintain, see docs/video-content.md).
            take.Morph(28.5f, 8f, "Intro Jungle Live")     // 8 beats, switch at beat 32.5, ends at 36.5
                .StartShape(TerrainShape.AudioShape.Square) // audio shape Circle becomes Square at once, the shape of Jungle follows at the switch
                .LateGlidePatternZoom(3f)                  // the pattern zoom stays for 3 beats and then glides smoothly over the remaining 5
                .ShapeStart(1.1f, 3.1f)                    // the mesh morph starts 1.1 beats after the start and glides for 3.1 s
                .Bump("TerrainHeight", 0.2f);              // terrain sinks around the switch

            return take;
        }

        private static void IntroPattern(DirectorTimeline take, float beat, TerrainNoise.Pattern pattern, float zoom)
        {
            take.Pattern(beat, pattern).Set(beat, "PerlinZoom", zoom);
        }
    }
}
