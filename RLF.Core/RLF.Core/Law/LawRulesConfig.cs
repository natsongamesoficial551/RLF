using System.Collections.Generic;
using RLF.Core.Identity.Enums;

namespace RLF.Core.Law
{
    /// <summary>
    /// Configuração central das regras legais.
    /// (FASE 4-A: valores fixos, depois vira INI)
    /// </summary>
    public static class LawRulesConfig
    {
        private static readonly Dictionary<ViolationType, LawRule> _rules
            = new Dictionary<ViolationType, LawRule>
        {
            {
                ViolationType.DrivingWithoutLicense,
                new LawRule(
                    violation: ViolationType.DrivingWithoutLicense,
                    action: LawActionType.Fine,
                    fineAmount: 250
                )
            },
            {
                ViolationType.WeaponWithoutPermit,
                new LawRule(
                    violation: ViolationType.WeaponWithoutPermit,
                    action: LawActionType.Fine,
                    fineAmount: 500
                )
            },
            {
                ViolationType.FlyingWithoutLicense,
                new LawRule(
                    violation: ViolationType.FlyingWithoutLicense,
                    action: LawActionType.Arrest,
                    fineAmount: 0,
                    requiresArrest: true
                )
            }
        };

        public static LawRule GetRule(ViolationType violation)
        {
            if (_rules.TryGetValue(violation, out var rule))
                return rule;

            // Default seguro
            return new LawRule(
                violation: violation,
                action: LawActionType.Warning,
                fineAmount: 0
            );
        }
    }
}
