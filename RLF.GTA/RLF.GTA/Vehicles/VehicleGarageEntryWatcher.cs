using System;
using System.Linq;
using GTA;
using GTA.Native;
using RLF.Core.Vehicles;

namespace RLF.GTA.Vehicles
{
    /// <summary>
    /// Detecção FINAL de garagem vanilla (com e sem teleporte).
    /// Baseada no comportamento REAL do GTA:
    /// - Player estava dirigindo
    /// - Veículo foi removido do mundo
    /// - Transição ocorreu recentemente
    /// </summary>
    public sealed class VehicleGarageEntryWatcher : Script
    {
        private string _lastPlate;
        private int _lastModel;
        private int _lastSeenGameTime;
        private bool _wasDrivingLastTick;

        // janela segura após dirigir (ms)
        private const int GARAGE_DETECTION_WINDOW = 8000;

        public VehicleGarageEntryWatcher()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            // 1) Cache enquanto dirige
            if (player.IsInVehicle())
            {
                Vehicle v = player.CurrentVehicle;
                if (v != null && v.Exists())
                {
                    CacheVehicle(v);
                    _wasDrivingLastTick = true;
                }
                return;
            }

            // 2) Não está mais dirigindo
            if (_wasDrivingLastTick)
            {
                // veículo sumiu logo após dirigir?
                if (RecentlyDriving() && !CachedVehicleExists())
                {
                    MarkVehicleAsGarage();
                }
            }

            _wasDrivingLastTick = false;
        }

        private void CacheVehicle(Vehicle v)
        {
            _lastPlate = GetPlateTextSafe(v);
            _lastModel = v.Model.Hash;
            _lastSeenGameTime = Game.GameTime;
        }

        private bool RecentlyDriving()
        {
            return (Game.GameTime - _lastSeenGameTime) <= GARAGE_DETECTION_WINDOW;
        }

        private bool CachedVehicleExists()
        {
            return World.GetAllVehicles().Any(v =>
            {
                try
                {
                    return v.Exists() &&
                           NormalizePlate(GetPlateTextSafe(v)) == NormalizePlate(_lastPlate);
                }
                catch
                {
                    return false;
                }
            });
        }

        private void MarkVehicleAsGarage()
        {
            var ownership = VehicleOwnershipBridge.Current;
            if (ownership == null)
                return;

            string normalized = NormalizePlate(_lastPlate);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            var targets = ownership.Vehicles
                .Where(v =>
                    v.State == VehicleState.World &&
                    NormalizePlate(v.Plate) == normalized
                )
                .ToList();

            if (targets.Count == 0)
                return;

            foreach (var v in targets)
                v.State = VehicleState.Garage;

            ownership.Save();

            global::GTA.UI.Notification.Show("🚗 Veículo guardado na garagem");
        }

        private static string GetPlateTextSafe(Vehicle vehicle)
        {
            try
            {
                return Function.Call<string>(
                    Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,
                    vehicle.Handle
                );
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizePlate(string plate)
        {
            return (plate ?? string.Empty).Trim().Replace(" ", "");
        }
    }
}
