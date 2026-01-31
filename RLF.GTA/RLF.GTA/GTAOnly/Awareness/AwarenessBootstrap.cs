using GTA;
using GTA.UI;
using System;
using System.Windows.Forms;
using RLF.GTA.GTAOnly.Awareness.Systems;
using RLF.GTA.GTAOnly.Awareness.Effects;

namespace RLF.GTA.GTAOnly.Awareness
{
    /// <summary>
    /// Mod Consciência Situacional (Standalone)
    /// PASSO 1: bootstrap + toggle
    /// PASSO 2: sistemas (lógica)
    /// PASSO 3: efeitos (câmera/áudio)
    /// </summary>
    public class AwarenessBootstrap : Script
    {
        private bool _enabled;

        // Systems (PASSO 2)
        private MovementAwareness _movement;
        private StressAwareness _stress;
        private FatigueAwareness _fatigue;
        private HeightAwareness _height;

        // Effects (PASSO 3)
        private CameraEffects _camera;
        private AudioEffects _audio;

        private DateTime _lastTick;

        public AwarenessBootstrap()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAborted;

            _enabled = true;

            _movement = new MovementAwareness();
            _stress = new StressAwareness();
            _fatigue = new FatigueAwareness();
            _height = new HeightAwareness();

            _camera = new CameraEffects();
            _audio = new AudioEffects();

            _lastTick = DateTime.Now;
        }

        private void OnTick(object sender, EventArgs e)
        {
            float dt = GetDeltaSeconds();

            if (!_enabled)
                return;

            // ===== PASSO 2 (lógica) =====
            if (AwarenessConfig.EnableMovementAwareness) _movement.Update();
            if (AwarenessConfig.EnableStressAwareness) _stress.Update();
            if (AwarenessConfig.EnableFatigueAwareness) _fatigue.Update();
            if (AwarenessConfig.EnableHeightAwareness) _height.Update();

            // ===== PASSO 3 (efeitos) =====
            float intensity = Clamp01(AwarenessConfig.GlobalIntensity);

            // Target FOV offset
            float targetFovOffset =
                (_movement.MovementIntensity * AwarenessConfig.MovementFovWeight) +
                (_stress.StressLevel * AwarenessConfig.StressFovWeight) +
                (_fatigue.FatigueLevel * AwarenessConfig.FatigueFovWeight);

            targetFovOffset *= intensity;

            // Target Shake (sutil + somatório ponderado)
            float targetShake =
                (_movement.MovementIntensity * AwarenessConfig.MovementShakeWeight) +
                (_stress.StressLevel * AwarenessConfig.StressShakeWeight) +
                (_fatigue.FatigueLevel * AwarenessConfig.FatigueShakeWeight) +
                (_height.HeightFactor * AwarenessConfig.HeightShakeWeight);

            targetShake *= intensity;

            _camera.Update(targetFovOffset, targetShake, dt);

            // Áudio (opcional/seguro)
            _audio.Update(_fatigue.FatigueLevel, _stress.StressLevel);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F9)
            {
                _enabled = !_enabled;

                if (!_enabled)
                {
                    // Reset imediato ao desativar
                    _camera.Reset();
                }

                Notification.Show($"~b~Awareness: ~w~{(_enabled ? "ON" : "OFF")}");
            }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            // Sempre reseta câmera para não ficar “preso”
            _camera.Reset();
        }

        private float GetDeltaSeconds()
        {
            var now = DateTime.Now;
            var dt = (float)(now - _lastTick).TotalSeconds;
            _lastTick = now;

            // segurança contra travadas
            if (dt < 0f) dt = 0f;
            if (dt > 0.25f) dt = 0.25f;
            return dt;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
