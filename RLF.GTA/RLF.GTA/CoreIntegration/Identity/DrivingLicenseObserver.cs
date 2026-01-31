using System;
using GTA;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using RLF.Core.Identity.Events;

namespace RLF.GTA.CoreIntegration.Identity
{
    public sealed class DrivingLicenseObserver : Script
    {
        private bool _wasDriverLastTick;

        public DrivingLicenseObserver()
        {
            _wasDriverLastTick = false;
            Tick += OnTick;

            RLFDebug.Info(DebugChannel.System, "DrivingLicenseObserver iniciado");
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            bool isDriver =
                player.IsInVehicle() &&
                player.CurrentVehicle != null &&
                player.CurrentVehicle.Driver == player;

            if (isDriver && !_wasDriverLastTick)
            {
                CheckDriverLicense();
            }

            _wasDriverLastTick = isDriver;
        }

        private void CheckDriverLicense()
        {
            try
            {
                var core = RLFCore.Instance;

                var docSystem = core.Systems.Get("DocumentSystem")
                    as RLF.Core.Identity.DocumentSystem;

                if (docSystem == null)
                {
                    RLFDebug.Warning(DebugChannel.System, "[CNH] DocumentSystem não encontrado");
                    return;
                }

                bool hasCNH =
                    docSystem.HasValidLicense(LicenseType.DriverCar) ||
                    docSystem.HasValidLicense(LicenseType.DriverMoto);

                // ✅ LOG DE DEBUG (remova depois de testar)
                RLFDebug.Info(
                    DebugChannel.System,
                    $"[CNH] Verificação: hasCNH={hasCNH}"
                );

                if (hasCNH)
                {
                    RLFDebug.Info(DebugChannel.System, "[CNH] Jogador TEM CNH válida - OK");
                    return;
                }

                // ❌ SEM CNH → VIOLAÇÃO
                ViolationType type = ViolationType.DrivingWithoutLicense;
                ViolationSeverity severity = ViolationSeverity.Major;

                core.RaiseEvent(
                    "identity:violation_detected",
                    new ViolationDetectedEvent(
                        type,
                        severity,
                        "Jogador dirigindo sem CNH valida"
                    )
                );

                RLFDebug.Warning(
                    DebugChannel.System,
                    "[CNH] VIOLAÇÃO: Dirigindo sem CNH"
                );
            }
            catch (Exception ex)
            {
                RLFDebug.Error(
                    DebugChannel.System,
                    "[CNH] Erro no DrivingLicenseObserver",
                    ex
                );
            }
        }
    }
}