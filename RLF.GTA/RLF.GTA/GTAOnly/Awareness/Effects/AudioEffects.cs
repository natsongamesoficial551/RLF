// RLF.GTA/GTAOnly/Awareness/Effects/AudioEffects.cs
using System;

namespace RLF.GTA.GTAOnly.Awareness.Effects
{
    /// <summary>
    /// Audio desativado por padrão e sem chamadas de API aqui,
    /// para garantir compatibilidade total (sem erros de assinatura).
    /// Estrutura e cooldown ficam prontos para ativarmos depois.
    /// </summary>
    public class AudioEffects
    {
        private DateTime _nextAllowed;

        public void Update(float fatigueLevel, float stressLevel)
        {
            if (!AwarenessConfig.EnableAudio)
                return;

            float trigger = Math.Max(fatigueLevel, stressLevel);

            if (trigger < AwarenessConfig.AudioTriggerThreshold)
                return;

            if (DateTime.Now < _nextAllowed)
                return;

            _nextAllowed = DateTime.Now.AddSeconds(AwarenessConfig.AudioCooldownSeconds);

            // (sem chamada de som aqui nesta versão — zero erros / máxima compatibilidade)
        }
    }
}
