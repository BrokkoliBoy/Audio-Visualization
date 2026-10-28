using System.Collections.Generic;
using System.Reflection;
using Gavi.Audio;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// The song the app starts with on this branch is the one of the take. <c>SimpleControl</c> reads its start index in Start, after this runs,
    /// so the private index is set through reflection and neither the script, the scene nor the order of the songs changes.
    /// </summary>
    public static class DirectorSong
    {
        public static void SelectStartSong(string songName)
        {
            SimpleControl control = Object.FindFirstObjectByType<SimpleControl>();
            FieldInfo clipsField = typeof(SimpleControl).GetField("_audioClips", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo indexField = typeof(SimpleControl).GetField("_audioStartIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            List<AudioClipContainer> clips = control != null && clipsField != null ? clipsField.GetValue(control) as List<AudioClipContainer> : null;
            if (clips == null || indexField == null)
            {
                Debug.LogWarning("Video Director: could not set the start song (SimpleControl or its fields not found).");
                return;
            }

            int index = clips.FindIndex(clip => clip != null && clip.name.ToLowerInvariant().Contains(songName.ToLowerInvariant()));
            if (index < 0)
            {
                Debug.LogWarning("Video Director: no song named '" + songName + "' in the song list.");
                return;
            }
            indexField.SetValue(control, index);
        }
    }
}
