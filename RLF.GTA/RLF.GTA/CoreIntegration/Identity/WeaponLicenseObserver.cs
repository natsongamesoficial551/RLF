using System;
using GTA;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using RLF.Core.Identity.Events;

namespace RLF.GTA.CoreIntegration.Identity
{
    /// <summary>
    /// Observer GTA: detecta jogador portando arma sem porte válido.
    /// Apenas dispara violação (sem punição).
    /// </summary>
    public sealed class WeaponLicenseObserver : Script
    {
        private bool _wasArmedLastTick;

        public WeaponLicenseObserver()
        {
            _wasArmedLastTick = false;
            Tick += OnTick;

            RLFDebug.Info(DebugChannel.System, "WeaponLicenseObserver iniciado");
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            bool isArmed =
                player.Weapons.Current != null &&
                player.Weapons.Current.Hash != WeaponHash.Unarmed;

            // Detecta quando saca a arma
            if (isArmed && !_wasArmedLastTick)
            {
                CheckWeaponPermit();
            }

            _wasArmedLastTick = isArmed;
        }

        private void CheckWeaponPermit()
        {
            try
            {
                var core = RLFCore.Instance;

                var docSystem = core.Systems.Get("DocumentSystem")
                    as RLF.Core.Identity.DocumentSystem;

                if (docSystem == null)
                    return;

                bool hasPermit = docSystem.HasValidLicense(LicenseType.WeaponPermit);
                if (hasPermit)
                    return;

                // ✅ Enum fixo existente no seu projeto
                ViolationType type = ViolationType.WeaponWithoutPermit;
                ViolationSeverity severity = ViolationSeverity.Major;

                core.RaiseEvent(
                    "identity:violation_detected",
                    new ViolationDetectedEvent(
                        type,
                        severity,
                        "Jogador portando arma sem porte valido"
                    )
                );

                RLFDebug.Warning(DebugChannel.System, "Violacao detectada: Porte de arma invalido");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "Erro no WeaponLicenseObserver", ex);
            }
        }
    }
}
