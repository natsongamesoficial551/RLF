using GTA;
using GTA.Native;
using RLF.Core.Needs;

namespace RLF.GTA.CoreIntegration.Needs
{
    public class NeedsFeedbackIntegration
    {
        private readonly NeedsSystem _needs;

        public NeedsFeedbackIntegration(NeedsSystem needs)
        {
            _needs = needs;
        }

        public void Tick()
        {
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists())
                return;

            float hunger = _needs.GetNeedValue(NeedType.Hunger);
            float thirst = _needs.GetNeedValue(NeedType.Thirst);
            float sleep = _needs.GetNeedValue(NeedType.Sleep);

            // 🍔 FOME — instabilidade leve
            if (hunger < 25f)
            {
                Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "SMALL_EXPLOSION_SHAKE", 0.05f);
            }

            // 💧 SEDE — sprint mais pesado
            if (thirst < 25f && ped.IsSprinting)
            {
                Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "HAND_SHAKE", 0.07f);
            }

            // 💤 SONO — dificuldade para manter sprint
            if (sleep < 20f && ped.IsSprinting)
            {
                // força o jogador a parar de sprintar
                ped.Task.ClearAllImmediately();
            }
        }
    }
}
