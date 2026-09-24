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
        private bool _isMic;
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
        }

        public void UnPause()
        {
            _audioSource.UnPause();
        }

        public void Stop()
        {
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
            _audioSource.GetSpectrumData(samples, channel, fftWindow);
        }
    }
}