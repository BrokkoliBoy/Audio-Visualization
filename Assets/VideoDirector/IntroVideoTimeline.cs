namespace Gavi.VideoDirector
{
    /// <summary>
    /// The script of the intro video for the GitHub README (about 15 s, hard cut at the end). It uses only the copies "Intro Thunderstorm Live" and "Intro Jungle Live"
    /// with their palettes, never the presets of the hero video or of next. Song and beats as in the hero video (Invincible, 100 BPM, 1 beat = 0.6 s).
    /// The details (start beat, patterns, spacing) are a first version and meant to be tuned.
    /// </summary>
    public static class IntroVideoTimeline
    {
        public const float StartBeat = 15f;
        public const float DropBeat = 18.5f;
        /// <summary>The patterns switch this many beats before the beat: damping and smoothing need a moment until the new pattern is clearly visible.</summary>
        public const float PatternLead = 0.25f;
        public const float PauseBeat = 42.5f;          // 16.5 s after the start

        public static DirectorTimeline Build()
        {
            DirectorTimeline take = new DirectorTimeline("Intro video")
            {
                Song = HeroVideoTimeline.SongName,
                StartBeat = StartBeat,
                EndBeat = 43f                              // after the pause; the beat stands still from the pause on, so the take stays (F10 ends it), fade and sound are done in DaVinci Resolve
            };

            take.StartMenu(true)                           // the menu is open from the first picture, so the viewer sees what can be changed
                .StartMenuTab("Terrain", true)             // on the Terrain tab, its windows folded: only the pattern (the thing that changes) is visible
                .StartWith("Intro Thunderstorm Live");

            // Four changes in front of the viewer, one every 2 beats after the drop. Zoom and depth are the numbers the slider shows; the zoom is converted to the value of the preset (handle position times 10 within the range of the pattern, see the values below).
            IntroPattern(take, DropBeat + 2f - PatternLead, TerrainNoise.Pattern.Voronoi, 0.2463f, 0.7f);          // zoom 5 (slider number, the middle of the handle)
            IntroPattern(take, DropBeat + 4f - PatternLead, TerrainNoise.Pattern.Interference, 0.7904f, 0.66f);   // zoom 7
            IntroPattern(take, DropBeat + 6f - PatternLead, TerrainNoise.Pattern.Bricks, 0.5522f);              // zoom 7.2
            IntroPattern(take, DropBeat + 8f - PatternLead, TerrainNoise.Pattern.Tiles, 0.9867f);               // zoom 8

            take.MenuTab(30.5f, "Color");                  // two beats after the start of the morph (on the beat grid, like the drop at 18.5) the menu shows the Color tab, where the colors change

            // Same recipe as the Thunderstorm to Jungle morph of the hero video (two places to maintain, see docs/video-content.md).
            take.Morph(28.5f, 8f, "Intro Jungle Live")     // 8 beats, switch at beat 32.5, ends at 36.5
                .StartShape(TerrainShape.AudioShape.Square) // audio shape Circle becomes Square at once, the shape of Jungle follows at the switch
                .LateGlidePatternZoom(3f)                  // the pattern zoom stays for 3 beats and then glides smoothly over the remaining 5
                .ShapeStart(1.1f, 3.1f)                    // the mesh morph starts 1.1 beats after the start and glides for 3.1 s
                .Bump("TerrainHeight", 0.2f);              // terrain sinks around the switch

            take.Menu(38.5f, false);                       // the menu closes
            take.Pause(PauseBeat);                         // the picture stands still like after the space key, so a bit of the end can be seen (cut in DaVinci Resolve)

            return take;
        }

        private static void IntroPattern(DirectorTimeline take, float beat, TerrainNoise.Pattern pattern, float zoom, float? depth = null, bool invert = false)
        {
            take.Pattern(beat, pattern).Set(beat, "PerlinZoom", zoom).Set(beat, "NoiseInvert", invert);   // only the start pattern of Intro Thunderstorm Live is inverted
            if (depth.HasValue)
                take.Set(beat, "PerlinDepth", depth.Value);
        }
    }
}
