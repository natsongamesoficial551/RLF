using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;
using RLF.Core.Safety;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Sistema de detecção automática de crimes cometidos pelo jogador no mundo.
    /// ✅ CORRIGIDO: Funciona com Character Creator (atualiza referência do player)
    /// ✅ INTEGRADO: Safety System para tick adaptativo
    /// </summary>
    public class CrimeDetection
    {
        #region Constants

        private const string SAFETY_SYSTEM_ID = "RLF.GTA.CrimeDetection";
        private const string SAFETY_DISPLAY_NAME = "Crime Detection";
        private const int NORMAL_TICK_MS = 100;      // 10x por segundo normal
        private const int REDUCED_TICK_MS = 300;     // ~3x por segundo reduzido
        private const int MINIMAL_TICK_MS = 1000;    // 1x por segundo mínimo

        private const float DETECTION_INTERVAL = 0.1f;

        #endregion

        #region Fields

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

        private float _detectionTimer;

        // 🛡️ Safety System
        private bool _registeredInSafety;
        private bool _usingSafetyTick;

        #endregion

        #region Properties

        public bool IsEnabled { get; set; }

        #endregion

        #region Constructor

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

            // 🛡️ Registra no Safety System
            RegisterInSafetySystem();

            CrimeLogger.Log("✅ CrimeDetection inicializado (Character Creator Compatible + Safety Integrated)");
        }

        #endregion

        #region Safety System Integration

        /// <summary>
        /// Registra o sistema no Safety System para tick adaptativo.
        /// </summary>
        private void RegisterInSafetySystem()
        {
            try
            {
                var safetyManager = SafeExecutionManager.Instance;

                // Verifica se o Safety está disponível e inicializado
                if (safetyManager == null || !safetyManager.IsInitialized)
                {
                    CrimeLogger.Log("⚠️ CrimeDetection: Safety System não disponível, usando tick manual");
                    _registeredInSafety = false;
                    _usingSafetyTick = false;
                    return;
                }

                // Registra com prioridade Normal e categoria Crime
                safetyManager.RegisterSystem(
                    systemId: SAFETY_SYSTEM_ID,
                    displayName: SAFETY_DISPLAY_NAME,
                    category: SystemCategory.Crime,
                    priority: TickPriority.Normal,
                    tickCallback: OnSafetyTick,
                    normalTickRateMs: NORMAL_TICK_MS,
                    reducedTickRateMs: REDUCED_TICK_MS,
                    minimalTickRateMs: MINIMAL_TICK_MS,
                    canRunCallback: CanCrimeDetectionRun
                );

                _registeredInSafety = true;
                _usingSafetyTick = true;

                CrimeLogger.Log($"✅ CrimeDetection: Registrado no Safety System (Normal: {NORMAL_TICK_MS}ms, Reduced: {REDUCED_TICK_MS}ms, Minimal: {MINIMAL_TICK_MS}ms)");
            }
            catch (Exception ex)
            {
                CrimeLogger.Log($"❌ CrimeDetection: Erro ao registrar no Safety System: {ex.Message}");
                _registeredInSafety = false;
                _usingSafetyTick = false;
            }
        }

        /// <summary>
        /// Remove o registro do Safety System.
        /// </summary>
        private void UnregisterFromSafetySystem()
        {
            if (!_registeredInSafety)
                return;

            try
            {
                SafeExecutionManager.Instance?.UnregisterSystem(SAFETY_SYSTEM_ID);
                _registeredInSafety = false;
                _usingSafetyTick = false;
                CrimeLogger.Log("✅ CrimeDetection: Removido do Safety System");
            }
            catch (Exception ex)
            {
                CrimeLogger.Log($"❌ CrimeDetection: Erro ao remover do Safety System: {ex.Message}");
            }
        }

        /// <summary>
        /// Callback que determina se o CrimeDetection pode rodar.
        /// Retorna false para pular o tick em certas condições.
        /// </summary>
        private bool CanCrimeDetectionRun()
        {
            // Não roda se desabilitado
            if (!IsEnabled) return false;

            // Não roda se player inválido
            Ped player = Player;
            if (player == null || !player.Exists() || !player.IsAlive)
                return false;

            return true;
        }

        /// <summary>
        /// Tick controlado pelo Safety System.
        /// Chamado com frequência adaptativa baseada no contexto do jogo.
        /// </summary>
        private void OnSafetyTick()
        {
            // Executa a detecção de crimes
            PerformCrimeDetection();
        }

        #endregion

        #region Update

        /// <summary>
        /// Método Update chamado externamente (ex: por um Script SHVDN).
        /// Se registrado no Safety, apenas valida. Se não, faz detecção manual.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            // ✅ CORRIGIDO: Valida player a cada frame (pode ter mudado)
            Ped player = Player;
            if (player == null || !player.Exists() || !player.IsAlive) return;

            // 🛡️ Se usando Safety System, o tick é controlado por lá
            if (_usingSafetyTick)
            {
                // O Safety System chama OnSafetyTick() automaticamente
                // Não precisa fazer nada aqui
                return;
            }

            // 🔄 Fallback: tick manual se Safety não disponível
            _detectionTimer += deltaTime;
            if (_detectionTimer < DETECTION_INTERVAL) return;
            _detectionTimer = 0f;

            PerformCrimeDetection();
        }

        /// <summary>
        /// Executa a lógica de detecção de crimes.
        /// Chamado pelo Safety System ou pelo Update manual.
        /// </summary>
        private void PerformCrimeDetection()
        {
            Ped player = Player;
            if (player == null || !player.Exists() || !player.IsAlive) return;

            DetectWeaponCrimes(player);
            DetectVehicleTheft(player);
            DetectAssaults(player);
        }

        #endregion

        #region Crime Detection Methods

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

        #endregion

        #region Helper Methods

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

        #endregion

        #region Shutdown

        public void Shutdown()
        {
            // 🛡️ Remove do Safety System
            UnregisterFromSafetySystem();

            _lastVehicle = null;
            _lastAssaultTarget = null;
            CrimeLogger.Log("🔄 CrimeDetection desligado");
        }

        #endregion
    }
}