using UnityEngine;
using UnityEngine.UI;

namespace Gavi.VideoDirector
{
    /// <summary>Black layer over the whole screen (menu included) for fading in and out. Built from code, lets every click through.</summary>
    public class DirectorFade
    {
        private readonly Image _image;

        /// <summary>0 = clear, 1 = black.</summary>
        public float Alpha
        {
            get => _image.color.a;
            set => _image.color = new Color(0f, 0f, 0f, Mathf.Clamp01(value));
        }

        public DirectorFade(Transform parent)
        {
            GameObject canvasObject = new GameObject("Director Fade", typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            GameObject imageObject = new GameObject("Black", typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            _image = imageObject.GetComponent<Image>();
            _image.raycastTarget = false;
            RectTransform rect = _image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Alpha = 0f;
        }
    }
}
