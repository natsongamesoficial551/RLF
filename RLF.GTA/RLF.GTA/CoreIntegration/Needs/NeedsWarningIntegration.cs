using GTA.UI;
using RLF.Core.Needs;
using System.Collections.Generic;

namespace RLF.GTA.CoreIntegration.Needs
{
    public class NeedsWarningIntegration
    {
        private readonly NeedsSystem _needs;

        // Guarda se já avisou para cada need
        private readonly Dictionary<NeedType, bool> _wasCritical;

        // Threshold padrão (pode ir para INI depois)
        private const float CriticalThreshold = 20f;

        public NeedsWarningIntegration(NeedsSystem needs)
        {
            _needs = needs;

            _wasCritical = new Dictionary<NeedType, bool>
            {
                { NeedType.Hunger, false },
                { NeedType.Thirst, false },
                { NeedType.Sleep, false }
            };
        }

        public void Tick()
        {
            CheckNeed(NeedType.Hunger, "Você está com muita fome");
            CheckNeed(NeedType.Thirst, "Você está com muita sede");
            CheckNeed(NeedType.Sleep, "Você precisa dormir");
        }

        private void CheckNeed(NeedType type, string message)
        {
            float value = _needs.GetNeedValue(type);
            bool isCritical = value <= CriticalThreshold;

            if (isCritical && !_wasCritical[type])
            {
                Notification.Show(message);
                _wasCritical[type] = true;
            }
            else if (!isCritical && _wasCritical[type])
            {
                // Reset quando sair do crítico
                _wasCritical[type] = false;
            }
        }
    }
}
