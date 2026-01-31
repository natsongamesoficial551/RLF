using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RLF.GTA.Vehicles
{
    public sealed class VehicleAbandonWatcher : Script
    {
        private const float DISTANCE_LIMIT = 300f;
        private const int TIME_LIMIT_MS = 60 * 1000;

        private readonly Dictionary<string, int> _abandonTimers = new Dictionary<string, int>();

        public VehicleAbandonWatcher()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            var ownership = VehicleOwnershipBridge.Current;
            if (ownership == null)
                return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            var worldVehicles = ownership.Vehicles
                .Where(v => v.State == VehicleState.World)
                .ToList();

            foreach (var data in worldVehicles)
            {
                // 🔒 evita race/crash enquanto o veículo está sendo guardado
                if (data.Id != Guid.Empty &&
                    VehicleGarageManualStore.StoringVehicleIds.Contains(data.Id))
                    continue;

                string plateKey = NormalizePlate(data.Plate);
                if (string.IsNullOrWhiteSpace(plateKey))
                    continue;

                Vehicle vehicle = FindWorldVehicleForData(data);

                // 🚨 veículo estava no mundo, mas SUMIU fisicamente
                if (vehicle == null || !vehicle.Exists())
                {
                    data.State = VehicleState.Impound;
                    ownership.Save();

                    global::GTA.UI.Notification.Show(
                        "🚓 Veículo foi recolhido para o pátio (não encontrado no mundo)"
                    );

                    _abandonTimers.Remove(plateKey);
                    continue;
                }

                // se alguém entrou no veículo, reseta timer
                if (!vehicle.IsSeatFree(VehicleSeat.Driver))
                {
                    _abandonTimers.Remove(plateKey);
                    continue;
                }

                float distance = vehicle.Position.DistanceTo(player.Position);

                // Se está perto, reseta timer
                if (distance < DISTANCE_LIMIT)
                {
                    _abandonTimers.Remove(plateKey);
                    continue;
                }

                // Está longe: inicia ou continua timer
                if (!_abandonTimers.ContainsKey(plateKey))
                {
                    _abandonTimers[plateKey] = Game.GameTime;
                    continue;
                }

                int elapsedTime = Game.GameTime - _abandonTimers[plateKey];

                if (elapsedTime >= TIME_LIMIT_MS)
                {
                    data.State = VehicleState.Impound;
                    ownership.Save();

                    global::GTA.UI.Notification.Show(
                        $"🚓 Veículo abandonado foi levado para o pátio"
                    );

                    _abandonTimers.Remove(plateKey);

                    // Processa apenas 1 veículo por tick
                    break;
                }
            }
        }

        private static Vehicle FindWorldVehicleForData(VehicleData data)
        {
            var all = World.GetAllVehicles()
                .Where(v => v != null && v.Exists())
                .ToArray();

            if (!string.IsNullOrWhiteSpace(data.Plate))
            {
                string target = NormalizePlate(data.Plate);

                foreach (var v in all)
                {
                    string p = GetPlateTextSafe(v);
                    if (!string.IsNullOrWhiteSpace(p) &&
                        NormalizePlate(p).Equals(target, StringComparison.OrdinalIgnoreCase))
                        return v;
                }
            }

            // Fallback: busca por modelo
            return all.FirstOrDefault(v => v.Model.Hash == data.Model);
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