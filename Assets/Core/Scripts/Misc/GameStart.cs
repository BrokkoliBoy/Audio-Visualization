using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Game
{
    public class GameStart : MonoBehaviour
    {

        [Tooltip("Not used any more, the frame rate limit comes from FrameRateSettings (monitor refresh rate or the saved value).")]
        [SerializeField] private int m_targetFrameRate = 144;
        [SerializeField] private float m_targetFixedDeltaTime = 0.02f;

        public static float TargetFrameRate;

        void Awake()
        {
            FrameRateSettings.Apply();
            if (m_targetFixedDeltaTime > 0)
                Time.fixedDeltaTime = m_targetFixedDeltaTime;
        }
    }
}