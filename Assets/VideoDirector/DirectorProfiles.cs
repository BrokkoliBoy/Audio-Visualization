using System;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>One video: its name, the preset the scene starts with and the script of the take. Every video has its own presets and palettes (see docs/video-content.md).</summary>
    public class VideoProfile
    {
        public string Name;
        public string StartPreset;
        public float StartVolumeSlider;
        public Func<DirectorTimeline> Build;
    }

    /// <summary>The videos the Director can play. F5 switches between them while no take runs, the choice is remembered between sessions.</summary>
    public static class DirectorProfiles
    {
        private const string ProfilePref = "VideoDirector.Profile";

        public static readonly VideoProfile[] All =
        {
            new VideoProfile { Name = "Hero", StartPreset = "Hero-Video Spacewarp Live", StartVolumeSlider = 0.62f, Build = HeroVideoTimeline.Build },
            new VideoProfile { Name = "Intro", StartPreset = "Intro Thunderstorm Live", StartVolumeSlider = 0.62f, Build = IntroVideoTimeline.Build }
        };

        private static int _index = -1;

        public static VideoProfile Current
        {
            get
            {
                if (_index < 0)
                {
                    string saved = PlayerPrefs.GetString(ProfilePref, All[0].Name);
                    _index = Mathf.Max(0, Array.FindIndex(All, p => p.Name == saved));
                }
                return All[_index];
            }
        }

        /// <summary>Takes the next video as the current one and remembers it.</summary>
        public static VideoProfile Next()
        {
            _index = (Array.IndexOf(All, Current) + 1) % All.Length;
            PlayerPrefs.SetString(ProfilePref, All[_index].Name);
            return All[_index];
        }
    }
}
