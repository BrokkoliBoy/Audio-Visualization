using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi
{
    /// <summary>
    /// Lens dirt of the terrain glow. The dirt texture only lights up where the bloom is, its strength is a fixed
    /// part plus a part that pulses with the beat. Both values are part of the terrain preset, the texture is a
    /// setting of the view.
    /// </summary>
    public class TerrainLensDirt : MonoBehaviour
    {
        public const float DefaultStrength = 0.1f;
        public const float DefaultBeat = 0.5f;
        public const float DefaultVignette = 0.25f;

        [Tooltip("Volume of the terrain view. Its profile is copied at runtime, so the profile asset stays unchanged.")]
        [SerializeField] private Volume _volume;
        [SerializeField] private Texture2D _dirtTexture;
        [Tooltip("Beat pulse between 0 and 1 that makes the dirt flash. Without it only the fixed strength is used.")]
        [SerializeField] private FloatModule _beatPulseModule;

        /// <summary>Dirt strength that is always there.</summary>
        public float Strength { get; set; } = DefaultStrength;
        /// <summary>Extra dirt strength at the peak of a beat.</summary>
        public float BeatAmount { get; set; } = DefaultBeat;

        /// <summary>Off: no dirt at all, the bloom itself is not touched.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Darkening of the screen edges, 0 = none, 1 = strongest. Not tied to the dirt switch.</summary>
        public float VignetteStrength { get; set; } = DefaultVignette;

        private Bloom _bloom;
        private Vignette _vignette;

        private void LateUpdate()
        {
            ApplyVignette();
            if (_bloom == null && (_volume == null || !_volume.profile.TryGet(out _bloom)))
                return;

            float pulse = _beatPulseModule != null ? Mathf.Clamp01(_beatPulseModule.GetValue()) : 0f;
            // Without a texture here the one from the volume profile stays.
            if (_dirtTexture != null)
                _bloom.dirtTexture.Override(_dirtTexture);
            _bloom.dirtIntensity.Override(Enabled ? Strength + BeatAmount * pulse : 0f);
        }

        private void ApplyVignette()
        {
            if (_volume == null)
                return;
            // The profile is a runtime copy, so the vignette is added to it when the profile has none.
            if (_vignette == null && !_volume.profile.TryGet(out _vignette))
                _vignette = _volume.profile.Add<Vignette>(true);

            _vignette.active = VignetteStrength > 0.001f;
            _vignette.intensity.Override(Mathf.Clamp01(VignetteStrength));
            _vignette.smoothness.Override(0.5f);
        }
    }
}
