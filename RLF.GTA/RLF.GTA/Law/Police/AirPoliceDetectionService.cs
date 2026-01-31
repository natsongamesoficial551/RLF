using GTA;
using GTA.Math;
using RLF.Core;
using RLF.Core.Identity;
using RLF.Core.Identity.Enums;

namespace RLF.GTA.Law.Police
{
    public sealed class AirPoliceDetectionService
    {
        private const float ILLEGAL_ALTITUDE = 200f; // metros
        private const float SAFE_ALTITUDE = 50f; // metros para considerar pouso

        public bool TryDetectIllegalFlight(out AirPoliceTarget target)
        {
            target = null;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return false;

            Vehicle aircraft = player.CurrentVehicle;
            if (aircraft == null || !aircraft.Exists())
                return false;

            // Só detecta se for aeronave
            if (!IsAircraft(aircraft))
                return false;

            // Verifica se tem CHT válido
            if (HasValidFlightLicense())
                return false;

            // Verifica altitude
            float groundZ = World.GetGroundHeight(player.Position);
            float altitude = player.Position.Z - groundZ;

            if (altitude > ILLEGAL_ALTITUDE)
            {
                target = new AirPoliceTarget(player, aircraft, altitude);
                return true;
            }

            return false;
        }

        public static bool IsLanding(Vehicle aircraft)
        {
            if (aircraft == null || !aircraft.Exists())
                return false;

            Ped player = Game.Player.Character;
            float groundZ = World.GetGroundHeight(player.Position);
            float altitude = player.Position.Z - groundZ;

            return altitude < SAFE_ALTITUDE;
        }

        private bool IsAircraft(Vehicle vehicle)
        {
            try
            {
                var model = vehicle.Model;
                return model.IsHelicopter || model.IsPlane;
            }
            catch
            {
                return false;
            }
        }

        private bool HasValidFlightLicense()
        {
            try
            {
                var docSystem = RLFCore.Instance?.Systems?.Get("DocumentSystem") as DocumentSystem;
                if (docSystem == null)
                    return false;

                return docSystem.HasValidLicense(LicenseType.PilotPlane) ||
                       docSystem.HasValidLicense(LicenseType.PilotHelicopter);
            }
            catch
            {
                return false;
            }
        }
    }
}