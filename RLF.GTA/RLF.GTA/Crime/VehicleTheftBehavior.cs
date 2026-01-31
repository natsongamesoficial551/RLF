using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Comportamento avançado de roubo de veículos.
    /// ✅ CORRIGIDO: Funciona com Character Creator
    /// </summary>
    public class VehicleTheftBehavior
    {
        private readonly CrimeSystem _crimeSystem;

        // ✅ CORRIGIDO: Usa propriedade dinâmica
        private Ped Player => Game.Player.Character;

        private Vehicle _lastEnteredVehicle;
        private DateTime _lastTheftCheckTime;
        private bool _wasInVehicleLastFrame;

        private const float CHECK_INTERVAL = 0.2f;
        private float _checkTimer;

        public bool IsEnabled { get; set; }

        public VehicleTheftBehavior(CrimeSystem crimeSystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));

            _lastEnteredVehicle = null;
            _lastTheftCheckTime = DateTime.Now;
            _wasInVehicleLastFrame = false;
            _checkTimer = 0f;
            IsEnabled = true;

            CrimeLogger.Log("✅ VehicleTheftBehavior inicializado (Character Creator Compatible)");
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            Ped player = Player;
            if (player == null || !player.Exists() || !player.IsAlive) return;

            _checkTimer += deltaTime;
            if (_checkTimer < CHECK_INTERVAL) return;
            _checkTimer = 0f;

            CheckVehicleEntry(player);
        }

        private void CheckVehicleEntry(Ped player)
        {
            bool inVehicleNow = player.IsInVehicle();

            if (inVehicleNow && !_wasInVehicleLastFrame)
            {
                Vehicle vehicle = player.CurrentVehicle;
                if (vehicle != null && vehicle.Exists())
                {
                    OnPlayerEnteredVehicle(player, vehicle);
                }
            }

            _wasInVehicleLastFrame = inVehicleNow;
        }

        private void OnPlayerEnteredVehicle(Ped player, Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return;
            if (player == null || !player.Exists()) return;
            if (player.SeatIndex != VehicleSeat.Driver) return;

            if (vehicle == _lastEnteredVehicle &&
                (DateTime.Now - _lastTheftCheckTime).TotalSeconds < 5.0)
            {
                return;
            }

            if (IsPlayerOwnedVehicle(vehicle))
            {
                return;
            }

            Ped previousDriver = GetPreviousDriver(vehicle, player);
            Ped[] passengers = vehicle.Occupants;

            bool hasOccupants = (previousDriver != null && previousDriver.Exists()) ||
                               (passengers != null && passengers.Length > 1);

            if (hasOccupants)
            {
                ProcessCarjacking(player, vehicle, previousDriver, passengers);
            }
            else
            {
                ProcessVehicleTheft(player, vehicle);
            }

            _lastEnteredVehicle = vehicle;
            _lastTheftCheckTime = DateTime.Now;
        }

        private void ProcessCarjacking(Ped player, Vehicle vehicle, Ped previousDriver, Ped[] passengers)
        {
            if (vehicle == null || !vehicle.Exists()) return;

            Vector3 pos = vehicle.Position;
            string zone = GetZoneName(pos);
            string location = GetStreetName(pos);

            CrimeRecord crime = _crimeSystem.RegisterCrime(
                CoreCrimeType.VehicleCarjacking,
                pos.X, pos.Y, pos.Z,
                location, zone
            );

            if (crime == null) return;

            crime.AddFlag(CrimeFlags.Violent);
            crime.MonetaryValue = GetVehicleValue(vehicle);

            if (player.Weapons.Current != null &&
                player.Weapons.Current.Hash != WeaponHash.Unarmed)
            {
                crime.AddFlag(CrimeFlags.WeaponUsed);
            }

            string vehicleModel = GetVehicleDisplayName(vehicle);
            string vehiclePlate = GetVehiclePlate(vehicle);
            _crimeSystem.IdentifySuspectVehicle(crime, vehicleModel, vehiclePlate);

            if (previousDriver != null && previousDriver.Exists())
            {
                ApplyVictimReaction(previousDriver, player);
                crime.Evidence.AddWitness($"driver_{previousDriver.Handle}");
            }

            if (passengers != null)
            {
                foreach (Ped passenger in passengers)
                {
                    if (passenger == null || !passenger.Exists()) continue;
                    if (passenger == player) continue;

                    ApplyVictimReaction(passenger, player);
                    crime.Evidence.AddWitness($"passenger_{passenger.Handle}");
                }
            }

            CrimeLogger.Log($"🚗 Carjacking: {vehicleModel} ({vehiclePlate})");
        }

        private void ProcessVehicleTheft(Ped player, Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return;

            if (!WasParkedOrUnoccupied(vehicle)) return;

            Vector3 pos = vehicle.Position;
            string zone = GetZoneName(pos);
            string location = GetStreetName(pos);

            CrimeRecord crime = _crimeSystem.RegisterCrime(
                CoreCrimeType.VehicleTheft,
                pos.X, pos.Y, pos.Z,
                location, zone
            );

            if (crime == null) return;

            crime.MonetaryValue = GetVehicleValue(vehicle);

            string vehicleModel = GetVehicleDisplayName(vehicle);
            string vehiclePlate = GetVehiclePlate(vehicle);
            _crimeSystem.IdentifySuspectVehicle(crime, vehicleModel, vehiclePlate);

            CrimeLogger.Log($"🔑 VehicleTheft: {vehicleModel} ({vehiclePlate})");
        }

        private void ApplyVictimReaction(Ped victim, Ped player)
        {
            if (victim == null || !victim.Exists()) return;
            if (victim.IsDead || !victim.IsAlive) return;

            Random rng = new Random(victim.Handle);
            float roll = (float)rng.NextDouble();

            if (roll < 0.7f)
            {
                victim.Task.ClearAll();
                Vector3 fleeDirection = victim.Position - player.Position;
                fleeDirection.Normalize();
                Vector3 fleeTarget = victim.Position + fleeDirection * 50f;
                victim.Task.RunTo(fleeTarget);
            }
            else if (roll < 0.85f)
            {
                victim.Task.HandsUp(5000);
            }
            else
            {
                if (!victim.Weapons.HasWeapon(WeaponHash.Pistol))
                {
                    victim.Weapons.Give(WeaponHash.Pistol, 30, false, true);
                }
                victim.Task.FightAgainst(player);
            }
        }

        private bool IsPlayerOwnedVehicle(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return false;
            return false;
        }

        private Ped GetPreviousDriver(Vehicle vehicle, Ped player)
        {
            if (vehicle == null || !vehicle.Exists()) return null;

            Ped[] occupants = vehicle.Occupants;
            if (occupants == null || occupants.Length == 0) return null;

            foreach (Ped occupant in occupants)
            {
                if (occupant == null || !occupant.Exists()) continue;
                if (occupant == player) continue;
                if (occupant.SeatIndex == VehicleSeat.Driver) return occupant;
            }

            return null;
        }

        private bool WasParkedOrUnoccupied(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return false;

            bool isLocked = Function.Call<int>(Hash.GET_VEHICLE_DOOR_LOCK_STATUS, vehicle.Handle) > 1;
            bool hasAlarm = Function.Call<bool>(Hash.IS_VEHICLE_ALARM_ACTIVATED, vehicle.Handle);

            return !isLocked || hasAlarm;
        }

        private float GetVehicleValue(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return 0f;
            return Function.Call<float>(Hash.GET_VEHICLE_MODEL_VALUE, vehicle.Model.Hash);
        }

        private string GetVehicleDisplayName(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return "Unknown";
            string displayName = Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, vehicle.Model.Hash);
            return displayName ?? vehicle.Model.Hash.ToString();
        }

        private string GetVehiclePlate(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return "UNKNOWN";
            return Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, vehicle.Handle);
        }

        private string GetZoneName(Vector3 position)
        {
            return Function.Call<string>(Hash.GET_NAME_OF_ZONE, position.X, position.Y, position.Z);
        }

        private string GetStreetName(Vector3 position)
        {
            OutputArgument streetHash = new OutputArgument();
            OutputArgument crossingHash = new OutputArgument();

            Function.Call(Hash.GET_STREET_NAME_AT_COORD,
                position.X, position.Y, position.Z,
                streetHash, crossingHash);

            string streetName = Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY,
                streetHash.GetResult<int>());

            return streetName ?? "Unknown Street";
        }

        public void Shutdown()
        {
            _lastEnteredVehicle = null;
            CrimeLogger.Log("🔄 VehicleTheftBehavior desligado");
        }
    }
}