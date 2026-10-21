using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Black cover that the app starts behind and that fades out slowly, so the first image comes up from black.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BlackFade : MonoBehaviour
    {
        [Tooltip("Seconds the screen stays black before it starts to fade.")]
        [SerializeField] private float _holdSeconds = 1.5f;
        [SerializeField] private float _fadeSeconds = 2.5f;

        private CanvasGroup _group;
        private float _time;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01((_time - _holdSeconds) / Mathf.Max(0.01f, _fadeSeconds));
            _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, fade);
            if (fade >= 1f)
                gameObject.SetActive(false);
        }
    }
}
