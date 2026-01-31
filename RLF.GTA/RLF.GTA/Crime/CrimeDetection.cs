using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Sistema de detecção automática de crimes cometidos pelo jogador no mundo.
    /// ✅ CORRIGIDO: Funciona com Character Creator (atualiza referência do player)
    /// </summary>
    public class CrimeDetection
    {
        private readonly CrimeSystem _crimeSystem;

        // ✅ REMOVIDO: private readonly Ped _player; (era imutável)
        // ✅ NOVO: Usa propriedade dinâmica
        private Ped Player => Game.Player.Character;

        private bool _wasAimingWeapon;
        private bool _wasInVehicle;
        private Vehicle _lastVehicle;
        private DateTime _lastVehicleCheckTime;
        private DateTime _lastWeaponCheckTime;
        private DateTime _lastAssaultCheckTime;
        private Ped _lastAssaultTarget;
        private int _lastPlayerHealth;

        private const float DETECTION_INTERVAL = 0.1f;
        private float _detectionTimer;

        public bool IsEnabled { get; set; }

        public CrimeDetection(CrimeSystem crimeSystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));

            // ✅ REMOVIDO: _player = Game.Player.Character; (inicialização inválida)

            _wasAimingWeapon = false;
            _wasInVehicle = false;
            _lastVehicle = null;
            _lastVehicleCheckTime = DateTime.Now;
            _lastWeaponCheckTime = DateTime.Now;
            _lastAssaultCheckTime = DateTime.Now;
            _lastAssaultTarget = null;

            // ✅ CORRIGIDO: Inicializa health de forma segura
            _lastPlayerHealth = Player != null && Player.Exists() ? Player.Health : 100;

            _detectionTimer = 0f;
            IsEnabled = true;

            CrimeLogger.Log("✅ CrimeDetection inicializado (Character Creator Compatible)");
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            // ✅ CORRIGIDO: Valida player a cada frame (pode ter mudado)
            Ped player = Player;
            if (player == null || !player.Exists() || !player.IsAlive) return;

            _detectionTimer += deltaTime;
            if (_detectionTimer < DETECTION_INTERVAL) return;
            _detectionTimer = 0f;

            DetectWeaponCrimes(player);
            DetectVehicleTheft(player);
            DetectAssaults(player);
        }

        private void DetectWeaponCrimes(Ped player)
        {
            if (player == null || !player.Exists()) return;

            bool isAiming = player.IsAiming;
            Weapon currentWeapon = player.Weapons.Current;

            if (currentWeapon == null) return;

            bool hasFirearm = currentWeapon.Group == WeaponGroup.Pistol ||
                             currentWeapon.Group == WeaponGroup.SMG ||
                             currentWeapon.Group == WeaponGroup.AssaultRifle ||
                             currentWeapon.Group == WeaponGroup.Shotgun ||
                             currentWeapon.Group == WeaponGroup.Sniper ||
                             currentWeapon.Group == WeaponGroup.Heavy;

            if (!hasFirearm) return;

            if (isAiming && !_wasAimingWeapon)
            {
                DetectWeaponThreat(player);
            }

            if (player.IsShooting && (DateTime.Now - _lastWeaponCheckTime).TotalSeconds > 1.0)
            {
                DetectPublicGunfire(player);
                _lastWeaponCheckTime = DateTime.Now;
            }

            _wasAimingWeapon = isAiming;
        }

        private void DetectWeaponThreat(Ped player)
        {
            if (!IsNearCivilians(player, 30f)) return;

            Vector3 pos = player.Position;
            string zone = GetZoneName(pos);
            string location = GetStreetName(pos);

            var crime = _crimeSystem.RegisterCrime(
                CoreCrimeType.WeaponThreat,
                pos.X, pos.Y, pos.Z,
                location, zone
            );

            if (crime != null)
            {
                crime.AddFlag(CrimeFlags.WeaponUsed);
                CrimeLogger.Log($"🔫 WeaponThreat detectado em {location}");
            }
        }

        private void DetectPublicGunfire(Ped player)
        {
            Vector3 pos = player.Position;
            string zone = GetZoneName(pos);
            string location = GetStreetName(pos);

            var crime = _crimeSystem.RegisterCrime(
                CoreCrimeType.PublicGunfire,
                pos.X, pos.Y, pos.Z,
                location, zone
            );

            if (crime != null)
            {
                crime.AddFlag(CrimeFlags.WeaponUsed);
                CrimeLogger.Log($"💥 PublicGunfire detectado em {location}");
            }
        }

        private void DetectVehicleTheft(Ped player)
        {
            if (player == null || !player.Exists()) return;

            bool inVehicle = player.IsInVehicle();

            if (inVehicle && !_wasInVehicle)
            {
                Vehicle currentVehicle = player.CurrentVehicle;
                if (currentVehicle != null && currentVehicle.Exists())
                {
                    bool isPlayerDriver = player.SeatIndex == VehicleSeat.Driver;

                    if (isPlayerDriver && !IsPlayerOwnedVehicle(currentVehicle))
                    {
                        bool hadOwner = HasNPCOwner(currentVehicle);

                        if (hadOwner)
                        {
                            DetectCarjacking(player, currentVehicle);
                        }
                        else
                        {
                            DetectVehicleTheftSimple(player, currentVehicle);
                        }

                        _lastVehicle = currentVehicle;
                        _lastVehicleCheckTime = DateTime.Now;
                    }
                }
            }

            _wasInVehicle = inVehicle;
        }

        private void DetectCarjacking(Ped player, Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return;

            Vector3 pos = vehicle.Position;
            string zone = GetZoneName(pos);
            string location = GetStreetName(pos);

            var crime = _crimeSystem.RegisterCrime(
                CoreCrimeType.VehicleCarjacking,
                pos.X, pos.Y, pos.Z,
                location, zone
            );

            if (crime != null)
            {
                crime.AddFlag(CrimeFlags.Violent);
                crime.MonetaryValue = GetVehicleValue(vehicle);

                if (player.Weapons.Current != null &&
                    player.Weapons.Current.Hash != WeaponHash.Unarmed)
                {
                    crime.AddFlag(CrimeFlags.WeaponUsed);
                }

                CrimeLogger.Log($"🚗 Carjacking detectado: {GetVehicleDisplayName(vehicle)}");
            }
        }

        private void DetectVehicleTheftSimple(Ped player, Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return;

            Vector3 pos = vehicle.Position;
            string zone = GetZoneName(pos);
            string location = GetStreetName(pos);

            var crime = _crimeSystem.RegisterCrime(
                CoreCrimeType.VehicleTheft,
                pos.X, pos.Y, pos.Z,
                location, zone
            );

            if (crime != null)
            {
                crime.MonetaryValue = GetVehicleValue(vehicle);
                CrimeLogger.Log($"🔑 VehicleTheft detectado: {GetVehicleDisplayName(vehicle)}");
            }
        }

        private void DetectAssaults(Ped player)
        {
            if (player == null || !player.Exists()) return;

            int currentHealth = player.Health;

            bool isInMeleeCombat = player.IsInMeleeCombat;

            if (isInMeleeCombat)
            {
                Ped[] nearbyPeds = World.GetNearbyPeds(player, 3f);

                if (nearbyPeds != null && nearbyPeds.Length > 0)
                {
                    foreach (Ped ped in nearbyPeds)
                    {
                        if (ped == null || !ped.Exists()) continue;
                        if (ped == player) continue;
                        if (ped.IsDead) continue;
                        if (IsCop(ped)) continue;

                        if (ped.IsInMeleeCombat && ped.Health < ped.MaxHealth)
                        {
                            if (_lastAssaultTarget != ped)
                            {
                                DetectPhysicalAssault(player, ped);
                                _lastAssaultTarget = ped;
                                _lastAssaultCheckTime = DateTime.Now;
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                if (_lastAssaultTarget != null &&
                    (DateTime.Now - _lastAssaultCheckTime).TotalSeconds > 2.0)
                {
                    _lastAssaultTarget = null;
                }
            }

            _lastPlayerHealth = currentHealth;
        }

        private void DetectPhysicalAssault(Ped player, Ped victim)
        {
            if (victim == null || !victim.Exists()) return;

            Vector3 pos = player.Position;
            string zone = GetZoneName(pos);
            string location = GetStreetName(pos);

            var crime = _crimeSystem.RegisterCrime(
                CoreCrimeType.PhysicalAssault,
                pos.X, pos.Y, pos.Z,
                location, zone
            );

            if (crime != null)
            {
                if (victim.IsDead)
                {
                    crime.AddFlag(CrimeFlags.VictimKilled);
                    CrimeLogger.Log($"💀 PhysicalAssault (Fatal) em {location}");
                }
                else if (victim.Health < victim.MaxHealth * 0.8f)
                {
                    crime.AddFlag(CrimeFlags.VictimInjured);
                    CrimeLogger.Log($"🤜 PhysicalAssault (Injured) em {location}");
                }
            }
        }

        private bool IsNearCivilians(Ped player, float radius)
        {
            if (player == null || !player.Exists()) return false;

            Ped[] nearbyPeds = World.GetNearbyPeds(player, radius);
            if (nearbyPeds == null) return false;

            foreach (Ped ped in nearbyPeds)
            {
                if (ped == null || !ped.Exists()) continue;
                if (ped == player) continue;
                if (!ped.IsAlive) continue;
                if (IsCop(ped)) continue;

                return true;
            }

            return false;
        }

        private bool IsPlayerOwnedVehicle(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return false;
            return false;
        }

        private bool HasNPCOwner(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return false;

            Ped player = Player;
            foreach (Ped ped in vehicle.Occupants)
            {
                if (ped != null && ped.Exists() && ped != player)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCop(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;
            return ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "COP");
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
            _lastVehicle = null;
            _lastAssaultTarget = null;
            CrimeLogger.Log("🔄 CrimeDetection desligado");
        }
    }
}