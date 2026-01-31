using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RLF.Core.Crime;
using RLF.Core.Crime.Establishments;
using RLF.Core.Economy;
using RLF.Core.Economy.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Controlador principal de assaltos a estabelecimentos.
    /// VERSÃO CORRIGIDA - Menu fixo sem piscar + Crash protection
    /// </summary>
    public class EstablishmentRobbery
    {
        private class ActiveRobbery
        {
            public EstablishmentData Establishment { get; set; }
            public Ped Clerk { get; set; }
            public bool ClerkComplying { get; set; }
            public bool AlarmTriggered { get; set; }
            public bool PoliceNotified { get; set; }
            public DateTime StartedAt { get; set; }
            public RobberyPhase Phase { get; set; }
            public float SafeProgress { get; set; }
            public decimal MoneyCollected { get; set; }
        }

        private enum RobberyPhase
        {
            None,
            Threatening,
            WaitingClerk,
            TakingRegister,
            OpeningSafe,
            Complete,
            Failed
        }

        private readonly CrimeSystem _crimeSystem;
        private readonly EconomySystem _economySystem;
        private readonly List<EstablishmentData> _establishments;
        private readonly Dictionary<int, Ped> _spawnedClerks;
        private readonly Dictionary<int, Ped> _clerkReferences; // NOVO: Cache de referências

        private ActiveRobbery _currentRobbery;
        private EstablishmentData _nearbyEstablishment;
        private float _checkTimer;

        // ✅ FIX MENU: Apenas uma variável para última mensagem
        private string _lastDisplayedMessage = "";

        private const float CHECK_INTERVAL = 0.1f;
        private const float INTERACTION_DISTANCE = 2.5f;
        private const float THREAT_DISTANCE = 25.0f;

        public bool IsEnabled { get; set; }
        public bool IsRobberyActive => _currentRobbery != null;

        public EstablishmentRobbery(CrimeSystem crimeSystem, EconomySystem economySystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));
            _economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));

            _establishments = EstablishmentDatabase.GetAllEstablishments();
            _spawnedClerks = new Dictionary<int, Ped>();
            _clerkReferences = new Dictionary<int, Ped>(); // NOVO

            _currentRobbery = null;
            _nearbyEstablishment = null;
            _checkTimer = 0f;

            IsEnabled = true;

            CrimeLogger.Log($"EstablishmentRobbery initialized (FIXED) with {_establishments.Count} establishments");
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _checkTimer += deltaTime;
            if (_checkTimer < CHECK_INTERVAL) return;
            _checkTimer = 0f;

            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists() || !player.IsAlive) return;

                if (_currentRobbery != null)
                {
                    UpdateActiveRobbery(player, deltaTime);
                }
                else
                {
                    CheckNearbyEstablishment(player);
                }

                ManageClerks(player);
                RenderCurrentMessage(); // ✅ Renderiza mensagem estável
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in EstablishmentRobbery.Update", ex);
            }
        }

        private void CheckNearbyEstablishment(Ped player)
        {
            try
            {
                Vector3 playerPos = player.Position;
                EstablishmentData nearest = EstablishmentDatabase.GetNearestEstablishment(playerPos, 50f);

                if (nearest != _nearbyEstablishment)
                {
                    _nearbyEstablishment = nearest;

                    if (nearest != null)
                    {
                        CrimeLogger.Log($"Player near establishment: {nearest.Name}");
                        SpawnClerkIfNeeded(nearest);
                    }
                }

                if (nearest != null && nearest.IsAvailable())
                {
                    float distance = playerPos.DistanceTo(nearest.Position);

                    if (distance <= THREAT_DISTANCE)
                    {
                        if (player.IsAiming && HasFirearm(player))
                        {
                            ShowRobberyPrompt(nearest, distance);

                            if (Game.IsControlJustPressed(Control.Context))
                            {
                                StartRobbery(player, nearest);
                            }
                        }
                        else
                        {
                            ClearMessage();
                        }
                    }
                    else
                    {
                        ClearMessage();
                    }
                }
                else
                {
                    ClearMessage();
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in CheckNearbyEstablishment", ex);
            }
        }

        private void StartRobbery(Ped player, EstablishmentData establishment)
        {
            try
            {
                if (establishment == null || !establishment.IsAvailable()) return;

                CrimeLogger.Log($"Starting robbery at {establishment.Name}");

                Ped clerk = GetClerkForEstablishment(establishment);

                _currentRobbery = new ActiveRobbery
                {
                    Establishment = establishment,
                    Clerk = clerk,
                    ClerkComplying = false,
                    AlarmTriggered = false,
                    PoliceNotified = false,
                    StartedAt = DateTime.Now,
                    Phase = RobberyPhase.Threatening,
                    SafeProgress = 0f,
                    MoneyCollected = 0m
                };

                establishment.State = EstablishmentState.BeingRobbed;

                RegisterRobberyCrime(player, establishment);
                MakeClerkReact(clerk, player);

                // ✅ Salva referência do clerk
                if (clerk != null && clerk.Exists())
                {
                    _clerkReferences[clerk.Handle] = clerk;
                }

                Notification.Show("~r~ASSALTO INICIADO!~n~~w~Continue mirando no atendente!");
                CrimeLogger.Log($"Robbery started at {establishment.Name}");
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in StartRobbery", ex);
            }
        }

        private void UpdateActiveRobbery(Ped player, float deltaTime)
        {
            try
            {
                if (_currentRobbery == null) return;

                EstablishmentData establishment = _currentRobbery.Establishment;
                Ped clerk = _currentRobbery.Clerk;

                if (player == null || !player.IsAlive)
                {
                    FailRobbery("Você morreu");
                    return;
                }

                float distance = player.Position.DistanceTo(establishment.Position);
                if (distance > 30f)
                {
                    FailRobbery("Você saiu da área");
                    return;
                }

                switch (_currentRobbery.Phase)
                {
                    case RobberyPhase.Threatening:
                        UpdateThreateningPhase(player, clerk);
                        break;

                    case RobberyPhase.WaitingClerk:
                        UpdateWaitingClerkPhase(clerk);
                        break;

                    case RobberyPhase.TakingRegister:
                        UpdateTakingRegisterPhase(player);
                        break;

                    case RobberyPhase.OpeningSafe:
                        UpdateOpeningSafePhase(player, deltaTime);
                        break;
                }

                CheckAlarmTrigger();
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in UpdateActiveRobbery", ex);
                FailRobbery("Erro no sistema");
            }
        }

        private void UpdateThreateningPhase(Ped player, Ped clerk)
        {
            try
            {
                if (clerk == null || !clerk.Exists() || clerk.IsDead)
                {
                    FailRobbery("Atendente morreu");
                    return;
                }

                if (!player.IsAiming)
                {
                    SetMessage("~r~Continue mirando no atendente!");
                    return;
                }

                if (IsClerkComplying(clerk))
                {
                    _currentRobbery.ClerkComplying = true;
                    _currentRobbery.Phase = RobberyPhase.WaitingClerk;
                    MakeClerkOpenRegister(clerk, _currentRobbery.Establishment);
                    Notification.Show("~g~Atendente rendido! Aguarde...");
                    CrimeLogger.Log("Clerk complying - moving to register");
                }
                else
                {
                    SetMessage("~y~Continue ameaçando o atendente...");
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in UpdateThreateningPhase", ex);
            }
        }

        private void UpdateWaitingClerkPhase(Ped clerk)
        {
            try
            {
                if (clerk == null || !clerk.Exists())
                {
                    FailRobbery("Atendente desapareceu");
                    return;
                }

                float distToCounter = clerk.Position.DistanceTo(_currentRobbery.Establishment.CounterPosition);

                if (distToCounter < 2.0f)
                {
                    _currentRobbery.Phase = RobberyPhase.TakingRegister;
                    Notification.Show("~g~O caixa está aberto!~n~~w~Vá pegar o dinheiro (pressione E)");
                    CrimeLogger.Log("Register opened - ready to collect");
                }
                else
                {
                    SetMessage("~y~Aguarde o atendente abrir o caixa...");
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in UpdateWaitingClerkPhase", ex);
            }
        }

        private void UpdateTakingRegisterPhase(Ped player)
        {
            try
            {
                Vector3 counterPos = _currentRobbery.Establishment.CounterPosition;
                float distance = player.Position.DistanceTo(counterPos);

                if (distance <= INTERACTION_DISTANCE)
                {
                    SetMessage("~g~Pressione E para pegar o dinheiro");

                    if (Game.IsControlJustPressed(Control.Context))
                    {
                        TakeRegisterMoney();
                    }
                }
                else
                {
                    SetMessage($"~y~Aproxime-se do balcão ({distance:F1}m)");
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in UpdateTakingRegisterPhase", ex);
            }
        }

        private void TakeRegisterMoney()
        {
            try
            {
                EstablishmentData establishment = _currentRobbery.Establishment;

                Random rng = new Random();
                decimal amount = establishment.MinCashRegister +
                               (establishment.MaxCashRegister - establishment.MinCashRegister) *
                               (decimal)rng.NextDouble();

                amount = Math.Round(amount / 10m) * 10m;

                CrimeLogger.Log($"Attempting to collect ${amount} from register");

                // ✅ ADICIONA AO DINHEIRO NATIVO DO GTA V
                Game.Player.Money += (int)amount;

                // ✅ ADICIONA AO ECONOMY SYSTEM (para tracking)
                var transaction = new EconomyTransaction(
                    amount: amount,
                    type: TransactionType.Income,
                    legality: TransactionLegality.Illegal,
                    origin: TransactionOrigin.RobberyStore,
                    description: $"Assalto: {establishment.Name} (Caixa)"
                );

                bool success = _economySystem.Wallet.ApplyTransaction(transaction);

                if (success)
                {
                    _currentRobbery.MoneyCollected += amount;
                    Notification.Show($"~g~+${amount:N0}~n~~w~Coletado do caixa!");
                    CrimeLogger.Log($"Collected ${amount} from register (GTA money: +{(int)amount})");

                    if (establishment.HasSafe)
                    {
                        _currentRobbery.Phase = RobberyPhase.OpeningSafe;
                        Notification.Show("~y~Tem um cofre! Vá abri-lo para pegar mais dinheiro!");
                    }
                    else
                    {
                        CompleteRobbery();
                    }
                }
                else
                {
                    CrimeLogger.LogError($"Failed to apply robbery transaction of ${amount}");
                    Notification.Show("~r~Erro ao coletar dinheiro!");
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("CRITICAL: Error in TakeRegisterMoney", ex);
                Notification.Show("~r~Erro crítico ao pegar dinheiro!");
                FailRobbery("Erro ao coletar dinheiro");
            }
        }

        private void UpdateOpeningSafePhase(Ped player, float deltaTime)
        {
            try
            {
                if (_currentRobbery.Establishment.SafePosition == Vector3.Zero)
                {
                    CompleteRobbery();
                    return;
                }

                Vector3 safePos = _currentRobbery.Establishment.SafePosition;
                float distance = player.Position.DistanceTo(safePos);

                if (distance <= INTERACTION_DISTANCE)
                {
                    if (Game.IsControlPressed(Control.Context))
                    {
                        _currentRobbery.SafeProgress += deltaTime / _currentRobbery.Establishment.SafeOpenTime;

                        int progressPercent = (int)(_currentRobbery.SafeProgress * 100f);
                        SetMessage($"~y~Abrindo cofre... {progressPercent}%");

                        if (_currentRobbery.SafeProgress >= 1.0f)
                        {
                            TakeSafeMoney();
                        }
                    }
                    else
                    {
                        SetMessage("~g~Segure E para abrir o cofre");
                    }
                }
                else
                {
                    SetMessage($"~y~Aproxime-se do cofre ({distance:F1}m)");
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in UpdateOpeningSafePhase", ex);
            }
        }

        private void TakeSafeMoney()
        {
            try
            {
                EstablishmentData establishment = _currentRobbery.Establishment;

                Random rng = new Random();
                decimal amount = establishment.MinSafeMoney +
                               (establishment.MaxSafeMoney - establishment.MinSafeMoney) *
                               (decimal)rng.NextDouble();

                amount = Math.Round(amount / 10m) * 10m;

                // ✅ ADICIONA AO DINHEIRO NATIVO DO GTA V
                Game.Player.Money += (int)amount;

                // ✅ ADICIONA AO ECONOMY SYSTEM (para tracking)
                var transaction = new EconomyTransaction(
                    amount: amount,
                    type: TransactionType.Income,
                    legality: TransactionLegality.Illegal,
                    origin: TransactionOrigin.RobberyStore,
                    description: $"Assalto: {establishment.Name} (Cofre)"
                );

                bool success = _economySystem.Wallet.ApplyTransaction(transaction);

                if (success)
                {
                    _currentRobbery.MoneyCollected += amount;
                    Notification.Show($"~g~+${amount:N0}~n~~w~Coletado do cofre!");
                    CrimeLogger.Log($"Collected ${amount} from safe (GTA money: +{(int)amount})");
                    CompleteRobbery();
                }
                else
                {
                    CrimeLogger.LogError($"Failed to apply safe robbery transaction of ${amount}");
                    CompleteRobbery(); // Completa mesmo com erro
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in TakeSafeMoney", ex);
                CompleteRobbery();
            }
        }

        private void CompleteRobbery()
        {
            try
            {
                if (_currentRobbery == null) return;

                decimal total = _currentRobbery.MoneyCollected;
                EstablishmentData establishment = _currentRobbery.Establishment;

                establishment.LastRobbedAt = DateTime.Now;
                establishment.State = EstablishmentState.Cooldown;

                Notification.Show($"~g~ASSALTO COMPLETO!~n~~w~Total: ${total:N0}");
                CrimeLogger.Log($"Robbery completed: ${total} from {establishment.Name}");

                CleanupRobbery();
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in CompleteRobbery", ex);
                CleanupRobbery();
            }
        }

        private void FailRobbery(string reason)
        {
            try
            {
                Notification.Show($"~r~ASSALTO FALHOU!~n~~w~{reason}");
                CrimeLogger.Log($"Robbery failed: {reason}");
                CleanupRobbery();
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in FailRobbery", ex);
            }
        }

        private void CleanupRobbery()
        {
            try
            {
                if (_currentRobbery != null)
                {
                    // Limpa referência do clerk
                    if (_currentRobbery.Clerk != null)
                    {
                        _clerkReferences.Remove(_currentRobbery.Clerk.Handle);
                    }

                    if (_currentRobbery.Establishment != null &&
                        _currentRobbery.Establishment.State == EstablishmentState.BeingRobbed)
                    {
                        _currentRobbery.Establishment.State = EstablishmentState.Available;
                    }
                }

                _currentRobbery = null;
                ClearMessage();
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in CleanupRobbery", ex);
            }
        }

        // ===== MESSAGE SYSTEM (FIX PISCAR) =====

        private void SetMessage(string message)
        {
            _lastDisplayedMessage = message;
        }

        private void ClearMessage()
        {
            _lastDisplayedMessage = "";
        }

        private void RenderCurrentMessage()
        {
            if (string.IsNullOrEmpty(_lastDisplayedMessage)) return;

            // ✅ USA SUBTITLE (como concessionária) - não pisca
            global::GTA.UI.Screen.ShowSubtitle(_lastDisplayedMessage, 100);
        }

        private void ShowRobberyPrompt(EstablishmentData establishment, float distance)
        {
            string message = $"~r~{establishment.Name}~n~~w~Pressione E para roubar";

            if (!establishment.IsAvailable())
            {
                TimeSpan cooldown = establishment.GetRemainingCooldown();
                message = $"~r~{establishment.Name}~n~~o~Roubado recentemente ({cooldown.TotalMinutes:F0}m)";
            }

            SetMessage(message);
        }

        // ===== CLERK MANAGEMENT =====

        private void SpawnClerkIfNeeded(EstablishmentData establishment)
        {
            try
            {
                if (establishment == null) return;

                int hash = establishment.Id.GetHashCode();
                if (_spawnedClerks.ContainsKey(hash))
                {
                    Ped existing = _spawnedClerks[hash];
                    if (existing != null && existing.Exists() && existing.IsAlive)
                        return;
                    else
                        _spawnedClerks.Remove(hash);
                }

                Model model = new Model(establishment.ClerkPedModel);
                model.Request();

                if (model.IsLoaded)
                {
                    Ped clerk = World.CreatePed(model, establishment.Position);
                    if (clerk != null && clerk.Exists())
                    {
                        clerk.Task.StandStill(-1);
                        clerk.IsInvincible = false;

                        _spawnedClerks[hash] = clerk;
                        _clerkReferences[clerk.Handle] = clerk; // ✅ Salva referência
                        CrimeLogger.Log($"Spawned clerk at {establishment.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error spawning clerk", ex);
            }
        }

        private Ped GetClerkForEstablishment(EstablishmentData establishment)
        {
            if (establishment == null) return null;

            int hash = establishment.Id.GetHashCode();
            if (_spawnedClerks.TryGetValue(hash, out Ped clerk))
            {
                if (clerk != null && clerk.Exists())
                    return clerk;
            }

            return null;
        }

        private void ManageClerks(Ped player)
        {
            try
            {
                List<int> toRemove = new List<int>();

                // ✅ USA CACHE DE REFERÊNCIAS
                foreach (var kvp in _clerkReferences.ToList())
                {
                    Ped clerk = kvp.Value;

                    if (clerk == null || !clerk.Exists() || clerk.IsDead)
                    {
                        toRemove.Add(kvp.Key);
                        continue;
                    }

                    float distance = clerk.Position.DistanceTo(player.Position);
                    if (distance > 100f)
                    {
                        clerk.Delete();
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (int key in toRemove)
                {
                    _clerkReferences.Remove(key);
                    _spawnedClerks.Remove(key);
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in ManageClerks", ex);
            }
        }

        private void MakeClerkReact(Ped clerk, Ped player)
        {
            if (clerk == null || !clerk.Exists()) return;

            clerk.Task.ClearAll();
            clerk.Task.HandsUp(60000);
            Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, clerk.Handle, player.Handle, 1000);
        }

        private void MakeClerkOpenRegister(Ped clerk, EstablishmentData establishment)
        {
            if (clerk == null || !clerk.Exists()) return;
            clerk.Task.GoTo(establishment.CounterPosition);
        }

        private bool IsClerkComplying(Ped clerk)
        {
            if (clerk == null || !clerk.Exists()) return false;

            int taskStatus = Function.Call<int>(Hash.GET_SCRIPT_TASK_STATUS, clerk.Handle, 0x8DCD9C87);
            return taskStatus >= 0 && taskStatus <= 7;
        }

        // ===== HELPERS =====

        private void CheckAlarmTrigger()
        {
            if (_currentRobbery == null || _currentRobbery.AlarmTriggered) return;

            Random rng = new Random();
            if (rng.NextDouble() < _currentRobbery.Establishment.AlarmTriggerChance)
            {
                _currentRobbery.AlarmTriggered = true;
                _currentRobbery.Establishment.State = EstablishmentState.Alarmed;
                Notification.Show("~r~ALARME DISPARADO!");
                CrimeLogger.Log("Alarm triggered");
            }
        }

        private bool HasFirearm(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;

            Weapon weapon = ped.Weapons.Current;
            if (weapon == null) return false;

            return weapon.Group == WeaponGroup.Pistol ||
                   weapon.Group == WeaponGroup.SMG ||
                   weapon.Group == WeaponGroup.AssaultRifle ||
                   weapon.Group == WeaponGroup.Shotgun;
        }

        private void RegisterRobberyCrime(Ped player, EstablishmentData establishment)
        {
            try
            {
                Vector3 pos = establishment.Position;
                string zone = Function.Call<string>(Hash.GET_NAME_OF_ZONE, pos.X, pos.Y, pos.Z);

                CrimeRecord crime = _crimeSystem.RegisterCrime(
                    CoreCrimeType.StoreRobbery,
                    pos.X, pos.Y, pos.Z,
                    establishment.Name,
                    zone
                );

                if (crime != null)
                {
                    crime.AddFlag(CrimeFlags.WeaponUsed);
                    crime.AddFlag(CrimeFlags.Violent);
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error registering robbery crime", ex);
            }
        }

        public void Shutdown()
        {
            try
            {
                foreach (var kvp in _spawnedClerks)
                {
                    if (kvp.Value != null && kvp.Value.Exists())
                    {
                        kvp.Value.Delete();
                    }
                }

                _spawnedClerks.Clear();
                _clerkReferences.Clear();
                ClearMessage();
                _currentRobbery = null;

                CrimeLogger.Log("EstablishmentRobbery shutdown");
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in Shutdown", ex);
            }
        }
    }
}