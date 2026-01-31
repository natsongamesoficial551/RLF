// RLF.GTA/GTAOnly/Awareness/Effects/CameraEffects.cs
using GTA.Native;
using System;

namespace RLF.GTA.GTAOnly.Awareness.Effects
{
    public class CameraEffects
    {
        private bool _initialized;
        private float _currentShake;

        public void InitializeIfNeeded()
        {
            if (_initialized) return;
            _currentShake = 0f;
            _initialized = true;
        }

        // Mantém a assinatura (pra não quebrar o Bootstrap), mas ignora FOV nesta versão
        public void Update(float targetFovOffset, float targetShake, float dt)
        {
            InitializeIfNeeded();

            targetShake = Clamp(targetShake, 0f, AwarenessConfig.MaxShake);
            _currentShake = SmoothTowards(_currentShake, targetShake, AwarenessConfig.ShakeSmoothing, dt);

            ApplyShake(_currentShake);
        }

        public void Reset()
        {
            if (!_initialized) return;
            StopShake();
            _currentShake = 0f;
        }

        private void ApplyShake(float intensity)
        {
            if (intensity <= 0.001f)
            {
                StopShake();
                return;
            }

            Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "HAND_SHAKE", intensity);
            Function.Call(Hash.SET_GAMEPLAY_CAM_SHAKE_AMPLITUDE, intensity);
        }

        private void StopShake()
        {
            Function.Call(Hash.STOP_GAMEPLAY_CAM_SHAKING, true);
            Function.Call(Hash.SET_GAMEPLAY_CAM_SHAKE_AMPLITUDE, 0f);
        }

        private static float SmoothTowards(float current, float target, float smoothness, float dt)
        {
            float t = 1f - (float)Math.Pow(1f - Clamp(smoothness, 0f, 1f), dt * 60f);
            return current + (target - current) * Clamp(t, 0f, 1f);
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
