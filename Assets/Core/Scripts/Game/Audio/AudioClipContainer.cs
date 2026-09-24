using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FunctionModules;

namespace Gavi.Audio
{
    //[CreateAssetMenu(fileName = "Audio Clip Container", menuName = "Scriptable Objects/Audio/Audio Clip Container")]
    public class AudioClipContainer : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;
        public AudioClip Clip => _clip;


        [Header("--- Audio Settings ---")]
        [SerializeField] private AudioClipSettings _audioSettings;
        public AudioClipSettings Settings => _audioSettings;

        [Tooltip("If null, no override will be done. If not null, the settings of this _overrideSampleSettings will be used instead of the SampleSettings of the SamplesContainer.")]
        [SerializeField] private AudioSampleSettingsPrefab _overrideSampleSettings;
        public AudioSampleSettingsPrefab OverrideSampleSettings => _overrideSampleSettings;


        [Header("--- Beat ---")]
        [SerializeField] private float _bpm;
        public float BPM => _bpm;

        [Tooltip("Offsets the impact of the beat by x seconds in respect to 0 seconds played.")]
        [SerializeField] private float _beatOffset;
        public float BeatOffset => _beatOffset;

        [Tooltip("The beatmap dictades how strong and how fast the beat should happen. t=0 and t=1 is the point in which the beat is hit, so you would" +
            "probably want the beat to happen right afterwards or transition out of it. WIth")]
        [SerializeField] private FunctionModule _beatMapModule;
        public FunctionModule BeatMapModule => _beatMapModule;

        [Header("--- Microphone ---")]
        [SerializeField] private bool _useMic;
        public bool UseMic => _useMic;
    }
}