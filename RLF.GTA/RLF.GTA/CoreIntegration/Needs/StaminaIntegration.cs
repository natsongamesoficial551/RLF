using GTA;
using GTA.Native;
using RLF.Core.Needs;
using System;

namespace RLF.GTA.CoreIntegration.Needs
{
    /// <summary>
    /// Integração de stamina com comportamento realista.
    /// - Sprint: dreno forte
    /// - Corrida: dreno leve
    /// - Parado/andando: recuperação
    /// - Stamina zero: bloqueia sprint
    /// </summary>
    public class StaminaIntegration
    {
        private readonly NeedsSystem _needs;

        // ===== TUNING =====
        private const float SprintDrainPerSecond = 0.9f;
        private const float RunDrainPerSecond = 0.25f;
        private const float RecoveryPerSecond = 0.35f;

        public StaminaIntegration(NeedsSystem needs)
        {
            _needs = needs ?? throw new ArgumentNullException(nameof(needs));
        }

        public void Tick()
        {
            var ped = Game.Player.Character;
            if (!ped.Exists())
                return;

            float deltaTime = Game.LastFrameTime;
            if (deltaTime <= 0f)
                return;

            float currentStamina = _needs.GetNeedValue(NeedType.Stamina);
            float newStamina = currentStamina;

            // ===== DRENO / RECUPERAÇÃO =====
            if (ped.IsSprinting)
            {
                newStamina -= SprintDrainPerSecond * deltaTime;
            }
            else if (ped.IsRunning)
            {
                newStamina -= RunDrainPerSecond * deltaTime;
            }
            else
            {
                newStamina += RecoveryPerSecond * deltaTime;
            }

            // Clamp de segurança
            newStamina = Math.Max(0f, Math.Min(100f, newStamina));

            // Atualiza o Core apenas se mudou
            float delta = newStamina - currentStamina;
            if (Math.Abs(delta) > 0.01f)
            {
                _needs.RestoreStamina(delta);
            }

            // ===== CONSEQUÊNCIA =====
            if (newStamina <= 0f && ped.IsSprinting)
            {
                Function.Call(Hash.SET_PLAYER_SPRINT, Game.Player, false);
            }
        }
    }
}
