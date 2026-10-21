using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Dark cover with a gliding bar for the first moments of the start, so the app does not look crashed while the first frames
    /// are slow (shaders, first mesh). It fades out slowly once the terrain has made its first mesh update.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingScreen : MonoBehaviour
    {
        [Tooltip("The part of the track that glides back and forth.")]
        [SerializeField] private RectTransform _bar;
        [Tooltip("The cover stays at least this long, so it does not flash by.")]
        [SerializeField] private float _minSeconds = 0.8f;
        [Tooltip("Fades out anyway after this long, in case the terrain never reports a mesh update.")]
        [SerializeField] private float _maxSeconds = 15f;
        [SerializeField] private float _fadeSeconds = 1.2f;
        [Tooltip("Seconds for the bar to glide from one end to the other.")]
        [SerializeField] private float _barSeconds = 1.4f;
        [Tooltip("Width of the bar as fraction of the track.")]
        [SerializeField] private float _barWidth = 0.3f;

        private CanvasGroup _group;
        private float _shown;
        private float _fade = -1f;
        private int _frames;
        private float _slowestFrame;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            _frames++;
            _shown += delta;
            _slowestFrame = Mathf.Max(_slowestFrame, delta);

            if (_bar != null)
            {
                float position = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(_shown / _barSeconds, 1f)) * (1f - _barWidth);
                _bar.anchorMin = new Vector2(position, 0f);
                _bar.anchorMax = new Vector2(position + _barWidth, 1f);
            }

            if (_fade < 0f)
            {
                if (_shown >= _minSeconds && _frames > 2 && (IsTerrainReady() || _shown >= _maxSeconds))
                {
                    _fade = 0f;
                    _group.blocksRaycasts = false;
                    Debug.Log("Loading screen: ready after " + Time.realtimeSinceStartup.ToString("F1") + " s since start, frame " + Time.frameCount + ", slowest frame " + (_slowestFrame * 1000f).ToString("F0") + " ms");
                }
                return;
            }

            _fade += delta / Mathf.Max(0.01f, _fadeSeconds);
            _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, _fade);
            if (_fade >= 1f)
                gameObject.SetActive(false);
        }

        private static bool IsTerrainReady()
        {
            TerrainSettingsManager manager = TerrainSettingsManager.Instance;
            return manager != null && manager.Terrain != null && manager.Terrain.UpdateMilliseconds > 0f;
        }
    }
}
