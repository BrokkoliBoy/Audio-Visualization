using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Gavi.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        public static AudioPlayer Instance;

        [SerializeField] private AudioSource _audioSource;

        [SerializeField] private AudioMixerGroup _mixerMusic;
        [SerializeField] private AudioMixerGroup _mixerMicrophone;


        public float CurrentClipLength => CurrentlyPlayingClip == null ? -1 : CurrentlyPlayingClip.Clip.length;
        public float CurrentPlaybackTime => _audioSource.clip == null ? -1 : _audioSource.time;
        public AudioClipContainer CurrentlyPlayingClip => _currentlyPlayingClip;
        private AudioClipContainer _currentlyPlayingClip;

        public bool IsPlaying => _audioSource.isPlaying;
        /// <summary>True while a song is playing or paused, so the terrain keeps the song's settings when paused.</summary>
        public bool IsPlayingOrPaused => _audioSource.isPlaying || _isPaused;
        public bool IsPaused => _isPaused;
        private bool _isPaused;
        private bool _isMic;
        // A paused source returns silence; the last spectrum is kept so the terrain keeps running on it.
        private readonly Dictionary<(int channel, int length), float[]> _lastSpectrum = new Dictionary<(int, int), float[]>();
        public float Volume
        {
            get => _audioSource.volume;
            set => _audioSource.volume = value;
        }


        #region Mono
        private void Awake()
        {
            if (Instance != null)
                Utility.Debugger.LogInstanceError(GetType());
            Instance = this;
        }
        #endregion


        #region Life Cycle
        public void Play (AudioClipContainer clip)
        {
            _currentlyPlayingClip = clip;
            _audioSource.Stop();
            _isPaused = false;
            _lastSpectrum.Clear();
            if (clip.UseMic)
                SwitchToMic();
            else
                SwitchToMusic(clip);
            _audioSource.Play();
        }

        public void Pause()
        {
            if (!_audioSource.isPlaying)
                return;
            _audioSource.Pause();
            _isPaused = true;
        }

        public void UnPause()
        {
            _isPaused = false;
            _audioSource.UnPause();
        }

        public void Stop()
        {
            _isPaused = false;
            _lastSpectrum.Clear();
            _audioSource.Stop();
            _currentlyPlayingClip = null;
        }

        public void Replay()
        {
            _audioSource.Stop();
            _audioSource.Play();
        }
        #endregion


        #region Microphone
        private void SwitchToMic()
        {
            if (Microphone.devices.Length <= 0)
                return;
            if (_isMic)
                return;
            _isMic = true;

            _audioSource.outputAudioMixerGroup = _mixerMicrophone;
            int deviceIndex = Mathf.Min(0, Microphone.devices.Length - 1);
            string deviceName = Microphone.devices[deviceIndex].ToString();
            _audioSource.clip = Microphone.Start(deviceName, true, 3599, AudioSettings.outputSampleRate);
            while (!(Microphone.GetPosition(null) > 0)) { } // to prevent the clip from being played only when the lengthSec is over
        }
        private void SwitchToMusic(AudioClipContainer clip)
        {
            _audioSource.outputAudioMixerGroup = _mixerMusic;
            _isMic = false;
            _audioSource.clip = clip.Clip;
        }
        #endregion


        public void GetSpectrumData(float[] samples, int channel, FFTWindow fftWindow)
        {
            var key = (channel, samples.Length);
            if (_isPaused && _lastSpectrum.TryGetValue(key, out float[] last))
            {
                System.Array.Copy(last, samples, samples.Length);
                return;
            }

            _audioSource.GetSpectrumData(samples, channel, fftWindow);
            if (!_audioSource.isPlaying)
                return;
            if (!_lastSpectrum.TryGetValue(key, out last))
                _lastSpectrum[key] = last = new float[samples.Length];
            System.Array.Copy(samples, last, samples.Length);
        }
    }
}