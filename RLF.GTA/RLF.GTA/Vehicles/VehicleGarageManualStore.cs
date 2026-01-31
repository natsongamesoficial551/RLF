using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RLF.Core.Vehicles;
using RLF.GTA.CoreIntegration;

namespace RLF.GTA.Vehicles
{
    public sealed class VehicleGarageManualStore : Script
    {
        private readonly Vector3 _garagePos = new Vector3(215.124f, -810.21f, 30.73f);

        private const float INTERACT_RADIUS = 2.0f;
        private const float MAX_VEHICLE_DISTANCE_FROM_GARAGE = 100f;

        private bool _menuOpen;
        private int _index;

        private int _nextNavAllowedAt;
        private int _nextSelectAllowedAt;

        private const int NAV_COOLDOWN_MS = 120;
        private const int SELECT_COOLDOWN_MS = 250;

        // 🔥 FIX: Controle manual da tecla K
        private bool _lastKState;

        internal static readonly HashSet<Guid> StoringVehicleIds = new HashSet<Guid>();

        public VehicleGarageManualStore()
        {
            Tick += OnTick;
            Aborted += OnAborted;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            // Só a pé
            if (player.IsInVehicle())
            {
                _menuOpen = false;
                _lastKState = false;
                return;
            }

            float distToPoint = player.Position.DistanceTo(_garagePos);

            if (distToPoint > INTERACT_RADIUS)
            {
                _menuOpen = false;
                _lastKState = false;
                return;
            }

            var ownership = VehicleOwnershipBridge.Current;
            if (ownership == null)
            {
                _menuOpen = false;
                _lastKState = false;
                return;
            }

            List<VehicleData> candidates = GetCandidates(ownership);

            if (candidates.Count == 0)
            {
                _menuOpen = false;
                _lastKState = false;
                global::GTA.UI.Screen.ShowHelpTextThisFrame("Nenhum veículo próximo da garagem para guardar");
                return;
            }

            if (!_menuOpen)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~y~K~s~ para guardar um veículo");
            }
            else
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame("↑↓ Escolher  |  ENTER Guardar  |  K Fechar");
            }

            // 🔥 FIX: Detecção correta de pressionamento (edge detection)
            bool kCurrentState = Game.IsKeyPressed(Keys.K);

            if (kCurrentState && !_lastKState)
            {
                // Tecla acabou de ser pressionada (rising edge)
                _menuOpen = !_menuOpen;
                if (_menuOpen)
                    _index = 0;
            }

            _lastKState = kCurrentState;

            if (!_menuOpen)
                return;

            if (_index >= candidates.Count) _index = 0;
            if (_index < 0) _index = candidates.Count - 1;

            DrawMenu(candidates);

            if (JustPressed(Keys.Up, ref _nextNavAllowedAt, NAV_COOLDOWN_MS))
                _index--;

            if (JustPressed(Keys.Down, ref _nextNavAllowedAt, NAV_COOLDOWN_MS))
                _index++;

            if (_index >= candidates.Count) _index = 0;
            if (_index < 0) _index = candidates.Count - 1;

            if (JustPressed(Keys.Enter, ref _nextSelectAllowedAt, SELECT_COOLDOWN_MS))
            {
                var selected = candidates[_index];

                bool ok = StoreVehicle(selected);
                if (!ok)
                    return;

                candidates = GetCandidates(ownership);
                if (candidates.Count == 0)
                {
                    _menuOpen = false;
                    _index = 0;
                    global::GTA.UI.Notification.Show("✅ Nenhum veículo restante para guardar");
                    return;
                }

                _index = 0;
            }
        }

        private void DrawMenu(List<VehicleData> list)
        {
            var selected = list[_index];

            global::GTA.UI.Screen.ShowSubtitle(
                $"GUARDAR VEÍCULO\n{_index + 1}/{list.Count}\nPlaca: {selected.Plate}\nModelo: {selected.Model}\nENTER Guardar",
                1
            );
        }

        private bool StoreVehicle(VehicleData data)
        {
            if (data == null)
                return false;

            if (data.State != VehicleState.World)
            {
                global::GTA.UI.Notification.Show("❌ Este veículo não está no mundo (State inválido)");
                return false;
            }

            if (data.Id != Guid.Empty)
                StoringVehicleIds.Add(data.Id);

            try
            {
                Vehicle v = FindVehicleNearGarage(data);
                if (v == null || !v.Exists())
                {
                    global::GTA.UI.Notification.Show("❌ Veículo não encontrado perto da garagem");
                    return false;
                }

                bool removed = SafeDeleteVehicle(v);
                if (!removed)
                {
                    global::GTA.UI.Notification.Show("❌ Falha ao remover o veículo (tente novamente)");
                    return false;
                }

                data.State = VehicleState.Garage;
                VehicleOwnershipBridge.Current.Save();

                global::GTA.UI.Notification.Show("🚗 Veículo guardado na garagem");
                return true;
            }
            finally
            {
                if (data.Id != Guid.Empty)
                    StoringVehicleIds.Remove(data.Id);
            }
        }

        private List<VehicleData> GetCandidates(VehicleOwnershipSystem ownership)
        {
            var worldVehicles = ownership.Vehicles
                .Where(v => v.State == VehicleState.World)
                .ToList();

            if (worldVehicles.Count == 0)
                return new List<VehicleData>();

            var result = new List<VehicleData>();
            Vehicle[] all = World.GetAllVehicles();

            foreach (var data in worldVehicles)
            {
                string targetPlate = NormalizePlate(data.Plate);
                if (string.IsNullOrWhiteSpace(targetPlate))
                    continue;

                foreach (var v in all)
                {
                    try
                    {
                        if (v == null || !v.Exists())
                            continue;

                        float dGarage = v.Position.DistanceTo(_garagePos);
                        if (dGarage > MAX_VEHICLE_DISTANCE_FROM_GARAGE)
                            continue;

                        string plate = NormalizePlate(GetPlateTextSafe(v));
                        if (plate.Equals(targetPlate, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(data);
                            break;
                        }
                    }
                    catch { }
                }
            }

            return result;
        }

        private Vehicle FindVehicleNearGarage(VehicleData data)
        {
            Vehicle best = null;
            float bestDist = float.MaxValue;

            string targetPlate = NormalizePlate(data.Plate);

            foreach (var v in World.GetAllVehicles())
            {
                try
                {
                    if (v == null || !v.Exists())
                        continue;

                    float d = v.Position.DistanceTo(_garagePos);
                    if (d > MAX_VEHICLE_DISTANCE_FROM_GARAGE)
                        continue;

                    string plate = NormalizePlate(GetPlateTextSafe(v));
                    if (!string.IsNullOrWhiteSpace(plate) &&
                        plate.Equals(targetPlate, StringComparison.OrdinalIgnoreCase))
                    {
                        if (d < bestDist)
                        {
                            best = v;
                            bestDist = d;
                        }
                    }
                }
                catch { }
            }

            return best;
        }

        private bool SafeDeleteVehicle(Vehicle v)
        {
            try
            {
                if (v == null || !v.Exists())
                    return true;

                try { v.IsPersistent = false; } catch { }
                try { v.MarkAsNoLongerNeeded(); } catch { }

                try
                {
                    if (!v.IsSeatFree(VehicleSeat.Driver))
                        v.Driver?.Task.ClearAllImmediately();
                }
                catch { }

                Vector3 farAway = new Vector3(10000f, 10000f, -200f);

                try
                {
                    v.Position = farAway;
                    v.Velocity = Vector3.Zero;
                }
                catch { }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool JustPressed(Keys key, ref int nextAllowedAt, int cooldownMs)
        {
            int now = Game.GameTime;
            if (now < nextAllowedAt)
                return false;

            if (!Game.IsKeyPressed(key))
                return false;

            nextAllowedAt = now + cooldownMs;
            return true;
        }

        private static string GetPlateTextSafe(Vehicle v)
        {
            try
            {
                return global::GTA.Native.Function.Call<string>(
                    global::GTA.Native.Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,
                    v.Handle
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

        private void OnAborted(object sender, EventArgs e)
        {
            _menuOpen = false;
            _index = 0;
            _lastKState = false;
            StoringVehicleIds.Clear();
        }
    }
}