using System;
using GTA;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using RLF.Core.Identity.Events;

namespace RLF.GTA.CoreIntegration.Identity
{
    /// <summary>
    /// Observer GTA: detecta jogador pilotando aeronave sem CHT válida.
    /// Apenas dispara violação (sem punição).
    /// </summary>
    public sealed class AircraftLicenseObserver : Script
    {
        private bool _wasPilotLastTick;

        public AircraftLicenseObserver()
        {
            _wasPilotLastTick = false;
            Tick += OnTick;

            RLFDebug.Info(DebugChannel.System, "AircraftLicenseObserver iniciado");
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            bool isPilot =
                player.IsInVehicle() &&
                player.CurrentVehicle != null &&
                player.CurrentVehicle.Driver == player &&
                IsAircraft(player.CurrentVehicle);

            // Só dispara quando entra na aeronave
            if (isPilot && !_wasPilotLastTick)
            {
                CheckPilotLicense();
            }

            _wasPilotLastTick = isPilot;
        }

        private void CheckPilotLicense()
        {
            try
            {
                var core = RLFCore.Instance;

                var docSystem = core.Systems.Get("DocumentSystem")
                    as RLF.Core.Identity.DocumentSystem;

                if (docSystem == null)
                    return;

                bool hasCHT = docSystem.HasValidLicense(LicenseType.PilotPlane);
                if (hasCHT)
                    return;

                // ✅ Enum fixo (agora existe no ViolationType.cs)
                ViolationType type = ViolationType.FlyingWithoutLicense;
                ViolationSeverity severity = ViolationSeverity.Major;

                core.RaiseEvent(
                    "identity:violation_detected",
                    new ViolationDetectedEvent(
                        type,
                        severity,
                        "Jogador pilotando aeronave sem CHT valida"
                    )
                );

                RLFDebug.Warning(DebugChannel.System, "Violacao detectada: Pilotagem sem CHT");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "Erro no AircraftLicenseObserver", ex);
            }
        }

        private static bool IsAircraft(Vehicle vehicle)
        {
            if (vehicle == null)
                return false;

            return vehicle.Model.IsPlane || vehicle.Model.IsHelicopter;
        }
    }
}
