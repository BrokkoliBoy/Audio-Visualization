using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Audio;

public class SimpleControl : MonoBehaviour
{
    [SerializeField] private KeyCode _keyCodeNextAudio = KeyCode.F1;
    [SerializeField] private KeyCode _keyCodePrevAudio = KeyCode.F2;
    [SerializeField] private AudioPlayer _audioPlayer;
    [SerializeField] private List<AudioClipContainer> _audioClips;
    [SerializeField] private int _audioStartIndex;
    
    private int _currentAudioIndex;


    private void Start()
    {
        if (_audioClips.Count <= 0)
            return;

        _currentAudioIndex = Mathf.Clamp(_audioStartIndex, 0, _audioClips.Count);
        _audioPlayer.Play(_audioClips[_audioStartIndex]);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_keyCodeNextAudio))
        {
            _currentAudioIndex++;
            _currentAudioIndex %= _audioClips.Count;

            _audioPlayer.Stop();
            _audioPlayer.Play(_audioClips[_currentAudioIndex]);

            Gavi.Events.Events.OnClipChanged?.Invoke();
        }
        else if (Input.GetKeyDown(_keyCodePrevAudio))
        {
            _currentAudioIndex--;
            if(_currentAudioIndex < 0)
                _currentAudioIndex = _audioClips.Count - 1;

            _audioPlayer.Stop();
            _audioPlayer.Play(_audioClips[_currentAudioIndex]);

            Gavi.Events.Events.OnClipChanged?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (_audioPlayer.IsPlaying)
                _audioPlayer.Pause();
            else
                _audioPlayer.UnPause();
        }

        if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Plus))
        {
            _audioPlayer.Volume = Mathf.Clamp01(_audioPlayer.Volume + Time.deltaTime * 0.3f);
        }
        if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
        {
            _audioPlayer.Volume = Mathf.Clamp01(_audioPlayer.Volume - Time.deltaTime * 0.3f);
        }
    }
}
