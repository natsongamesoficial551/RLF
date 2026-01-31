using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;
using RLF.Core.Economy;
using RLF.Core.Economy.Transactions;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Sistema de interação para assaltar NPCs na rua.
    /// ✅ CORRIGIDO: Funciona com Character Creator
    /// </summary>
    public class RobberyInteraction
    {
        private readonly CrimeSystem _crimeSystem;
        private readonly EconomySystem _economySystem;
        private readonly Dictionary<int, NPCWallet> _npcWallets;
        private readonly Dictionary<int, DateTime> _robbedNPCs;
        private readonly Dictionary<int, DateTime> _complyingNPCs;
        private readonly Dictionary<int, Ped> _complyingPedReferences;

        private Ped _currentTarget;
        private DateTime _lastRobberyCheck;
        private DateTime _lastNotificationTime;

        private const float ROBBERY_DISTANCE = 0.8f;
        private const float CHECK_INTERVAL = 0.3f;
        private float _checkTimer;

        public bool IsEnabled { get; set; }

        public RobberyInteraction(CrimeSystem crimeSystem, EconomySystem economySystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));
            _economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));

            _npcWallets = new Dictionary<int, NPCWallet>();
            _robbedNPCs = new Dictionary<int, DateTime>();
            _complyingNPCs = new Dictionary<int, DateTime>();
            _complyingPedReferences = new Dictionary<int, Ped>();

            _currentTarget = null;
            _lastRobberyCheck = DateTime.Now;
            _lastNotificationTime = DateTime.Now;
            _checkTimer = 0f;

            IsEnabled = true;

            CrimeLogger.Log("✅ RobberyInteraction inicializado (Character Creator Compatible)");
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _checkTimer += deltaTime;
            if (_checkTimer < CHECK_INTERVAL) return;
            _checkTimer = 0f;

            try
            {
                CheckForRobberyOpportunity();
                MaintainComplyingNPCs();
                CleanupOldData();
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in RobberyInteraction.Update", ex);
            }
        }

        private void MaintainComplyingNPCs()
        {
            try
            {
                List<int> toRemove = new List<int>();

                foreach (var kvp in _complyingPedReferences.ToList())
                {
                    int pedHandle = kvp.Key;
                    Ped ped = kvp.Value;

                    if (ped == null || !ped.Exists() || ped.IsDead || !ped.IsAlive)
                    {
                        toRemove.Add(pedHandle);
                        continue;
                    }

                    if (_complyingNPCs.ContainsKey(pedHandle))
                    {
                        if ((DateTime.Now - _complyingNPCs[pedHandle]).TotalSeconds > 30.0)
                        {
                            UnblockNPC(ped);
                            toRemove.Add(pedHandle);
                            continue;
                        }
                    }

                    Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped.Handle, true);

                    int taskStatus = Function.Call<int>(Hash.GET_SCRIPT_TASK_STATUS, ped.Handle, 0x8DCD9C87);

                    if (taskStatus == 7 || taskStatus == -1 || taskStatus < 0)
                    {
                        ped.Task.ClearAll();
                        ped.Task.HandsUp(-1);
                    }
                }

                foreach (int handle in toRemove)
                {
                    _complyingNPCs.Remove(handle);
                    _complyingPedReferences.Remove(handle);
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in MaintainComplyingNPCs", ex);
            }
        }

        private void UnblockNPC(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists()) return;

                Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped.Handle, false);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, ped.Handle, false);
                Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, ped.Handle, false);
                Function.Call(Hash.STOP_PED_SPEAKING, ped.Handle, false);
                ped.Task.ClearAll();

                CrimeLogger.Log($"NPC {ped.Handle} unblocked");
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError($"Error unblocking NPC", ex);
            }
        }

        private void CheckForRobberyOpportunity()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists() || !player.IsAlive)
                    return;

                if (!player.IsAiming)
                {
                    _currentTarget = null;
                    return;
                }

                Weapon currentWeapon = player.Weapons.Current;
                if (currentWeapon == null || !IsFirearm(currentWeapon))
                {
                    _currentTarget = null;
                    return;
                }

                Ped targetPed = GetNearestPedInFront(player);

                if (targetPed == null || !targetPed.Exists() || !targetPed.IsAlive)
                {
                    _currentTarget = null;
                    return;
                }

                float distance = player.Position.DistanceTo(targetPed.Position);
                if (distance > ROBBERY_DISTANCE * 2)
                {
                    _currentTarget = null;
                    return;
                }

                if (!IsNPCComplying(targetPed))
                {
                    _currentTarget = null;
                    return;
                }

                if (_robbedNPCs.ContainsKey(targetPed.Handle))
                {
                    _currentTarget = null;
                    return;
                }

                _currentTarget = targetPed;

                if (distance <= ROBBERY_DISTANCE)
                {
                    NPCWallet wallet = GetOrCreateWallet(targetPed);

                    if (wallet != null && wallet.HasMoney())
                    {
                        ExecuteRobbery(player, targetPed, wallet);
                    }
                    else
                    {
                        global::GTA.UI.Notification.Show("~r~Esta pessoa não tem dinheiro!");
                        _robbedNPCs[targetPed.Handle] = DateTime.Now;
                        _complyingNPCs.Remove(targetPed.Handle);
                        _complyingPedReferences.Remove(targetPed.Handle);
                        UnblockNPC(targetPed);
                    }
                }
                else
                {
                    ShowDistanceIndicator(targetPed, distance);
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in CheckForRobberyOpportunity", ex);
            }
        }

        private void ShowDistanceIndicator(Ped target, float distance)
        {
            try
            {
                if ((DateTime.Now - _lastNotificationTime).TotalSeconds < 1.0)
                    return;

                if (!_complyingNPCs.ContainsKey(target.Handle))
                {
                    _complyingNPCs[target.Handle] = DateTime.Now;
                    _complyingPedReferences[target.Handle] = target;
                }

                string distanceStr = distance.ToString("F1");
                global::GTA.UI.Notification.Show($"~y~Aproxime-se mais ({distanceStr}m)");
                _lastNotificationTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in ShowDistanceIndicator", ex);
            }
        }

        private void ExecuteRobbery(Ped player, Ped victim, NPCWallet wallet)
        {
            try
            {
                if (victim == null || !victim.Exists() || wallet == null)
                {
                    CrimeLogger.LogError("ExecuteRobbery called with null victim or wallet");
                    return;
                }

                decimal stolenAmount = wallet.Rob();

                if (stolenAmount <= 0m)
                {
                    CrimeLogger.Log($"Robbery failed - stolen amount is {stolenAmount}");
                    global::GTA.UI.Notification.Show("~r~Esta pessoa não tem dinheiro!");
                    _robbedNPCs[victim.Handle] = DateTime.Now;
                    _complyingNPCs.Remove(victim.Handle);
                    _complyingPedReferences.Remove(victim.Handle);
                    UnblockNPC(victim);
                    return;
                }

                Game.Player.Money += (int)stolenAmount;

                var transaction = new EconomyTransaction(
                    amount: stolenAmount,
                    type: TransactionType.Income,
                    legality: TransactionLegality.Illegal,
                    origin: TransactionOrigin.RobberyNPC,
                    description: $"Roubo de NPC (${stolenAmount})"
                );

                bool success = _economySystem.Wallet.ApplyTransaction(transaction);

                if (success)
                {
                    CrimeLogger.Log($"Robbery successful: ${stolenAmount} added (GTA money: +{(int)stolenAmount})");
                }
                else
                {
                    CrimeLogger.LogError($"Economy tracking failed (money still added to GTA)");
                }

                RegisterRobberyCrime(player, victim, stolenAmount);

                _robbedNPCs[victim.Handle] = DateTime.Now;
                _complyingNPCs.Remove(victim.Handle);
                _complyingPedReferences.Remove(victim.Handle);

                ApplyVictimReaction(victim, player);

                global::GTA.UI.Notification.Show($"~g~+${stolenAmount:F0}~w~ roubado!");

                CrimeLogger.Log($"Robbery completed: ${stolenAmount} from NPC {victim.Handle}");
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError($"Error in ExecuteRobbery", ex);
                global::GTA.UI.Notification.Show("~r~Erro ao executar roubo!");
            }
        }

        private void RegisterRobberyCrime(Ped player, Ped victim, decimal amount)
        {
            try
            {
                if (_crimeSystem == null || victim == null || !victim.Exists())
                    return;

                Vector3 pos = victim.Position;
                string zone = GetZoneName(pos);
                string location = GetStreetName(pos);

                CrimeRecord crime = _crimeSystem.RegisterCrime(
                    CoreCrimeType.PedestrianRobbery,
                    pos.X, pos.Y, pos.Z,
                    location, zone
                );

                if (crime != null)
                {
                    crime.AddFlag(CrimeFlags.Violent);
                    crime.AddFlag(CrimeFlags.Witnessed);
                    crime.AddFlag(CrimeFlags.WeaponUsed);
                    crime.MonetaryValue = (float)amount;

                    crime.Evidence.AddWitness($"victim_{victim.Handle}");

                    CrimeLogger.Log($"Crime registered: Robbery of ${amount}");
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error registering robbery crime", ex);
            }
        }

        private void ApplyVictimReaction(Ped victim, Ped player)
        {
            try
            {
                if (victim == null || !victim.Exists() || !victim.IsAlive)
                    return;

                UnblockNPC(victim);

                Random rng = new Random(victim.Handle);
                float roll = (float)rng.NextDouble();

                if (roll < 0.8f)
                {
                    victim.Task.ClearAll();
                    Vector3 fleeDirection = victim.Position - player.Position;
                    fleeDirection.Normalize();
                    Vector3 fleeTarget = victim.Position + fleeDirection * 50f;
                    victim.Task.RunTo(fleeTarget);

                    CrimeLogger.Log($"NPC {victim.Handle} fleeing");
                }
                else
                {
                    victim.Task.ClearAll();
                    victim.Task.Cower(-1);

                    CrimeLogger.Log($"NPC {victim.Handle} cowering");
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in ApplyVictimReaction", ex);
            }
        }

        private bool IsNPCComplying(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists() || !ped.IsAlive)
                    return false;

                bool isStationary = ped.Velocity.Length() < 0.5f;
                bool notFleeing = !Function.Call<bool>(Hash.IS_PED_FLEEING, ped.Handle);
                bool notRunning = !ped.IsRunning && !ped.IsSprinting;

                int taskStatus = Function.Call<int>(Hash.GET_SCRIPT_TASK_STATUS, ped.Handle, 0x8DCD9C87);
                bool hasHandsUpTask = (taskStatus >= 0 && taskStatus <= 7);

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return false;

                Vector3 directionToPed = ped.Position - player.Position;
                Vector3 pedForward = ped.ForwardVector;
                float dot = Vector3.Dot(pedForward.Normalized, directionToPed.Normalized);
                bool lookingAtPlayer = dot < -0.5f;

                bool isComplying = isStationary && notFleeing && notRunning && (hasHandsUpTask || lookingAtPlayer);

                if (isComplying)
                {
                    if (!_complyingNPCs.ContainsKey(ped.Handle))
                    {
                        _complyingNPCs[ped.Handle] = DateTime.Now;
                        _complyingPedReferences[ped.Handle] = ped;
                        CrimeLogger.Log($"Auto-registered NPC {ped.Handle} as complying");
                    }
                }

                return isComplying;
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in IsNPCComplying", ex);
                return false;
            }
        }

        private Ped GetNearestPedInFront(Ped player)
        {
            try
            {
                if (player == null || !player.Exists()) return null;

                Ped[] nearbyPeds = World.GetNearbyPeds(player, ROBBERY_DISTANCE * 2);
                if (nearbyPeds == null || nearbyPeds.Length == 0) return null;

                Ped closestPed = null;
                float closestDistance = float.MaxValue;
                Vector3 playerForward = player.ForwardVector;

                foreach (Ped ped in nearbyPeds)
                {
                    if (!IsValidRobberyTarget(ped, player)) continue;

                    Vector3 directionToPed = ped.Position - player.Position;
                    directionToPed.Normalize();

                    float dot = Vector3.Dot(playerForward, directionToPed);
                    if (dot < 0.5f) continue;

                    float distance = player.Position.DistanceTo(ped.Position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPed = ped;
                    }
                }

                return closestPed;
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in GetNearestPedInFront", ex);
                return null;
            }
        }

        private bool IsValidRobberyTarget(Ped ped, Ped player)
        {
            if (ped == null || !ped.Exists()) return false;
            if (ped == player) return false;
            if (!ped.IsAlive || ped.IsDead) return false;
            if (ped.IsInVehicle()) return false;
            if (IsCop(ped)) return false;

            return true;
        }

        private bool IsCop(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists()) return false;
                return ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "COP");
            }
            catch
            {
                return false;
            }
        }

        private bool IsFirearm(Weapon weapon)
        {
            if (weapon == null) return false;

            return weapon.Group == WeaponGroup.Pistol ||
                   weapon.Group == WeaponGroup.SMG ||
                   weapon.Group == WeaponGroup.AssaultRifle ||
                   weapon.Group == WeaponGroup.Shotgun ||
                   weapon.Group == WeaponGroup.Sniper ||
                   weapon.Group == WeaponGroup.Heavy;
        }

        private NPCWallet GetOrCreateWallet(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists()) return null;

                int handle = ped.Handle;

                if (_npcWallets.ContainsKey(handle))
                {
                    NPCWallet existing = _npcWallets[handle];
                    if (existing != null && existing.IsValid())
                    {
                        return existing;
                    }
                    else
                    {
                        _npcWallets.Remove(handle);
                    }
                }

                NPCWallet newWallet = new NPCWallet(ped);
                CrimeLogger.Log($"Created wallet for NPC {handle}: ${newWallet.Money}");

                if (newWallet.IsValid())
                {
                    _npcWallets[handle] = newWallet;
                    return newWallet;
                }

                return null;
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in GetOrCreateWallet", ex);
                return null;
            }
        }

        private void CleanupOldData()
        {
            try
            {
                List<int> walletsToRemove = new List<int>();
                foreach (var kvp in _npcWallets)
                {
                    if (kvp.Value == null || !kvp.Value.IsValid())
                    {
                        walletsToRemove.Add(kvp.Key);
                    }
                }
                foreach (int key in walletsToRemove)
                {
                    _npcWallets.Remove(key);
                }

                List<int> cooldownsToRemove = new List<int>();
                foreach (var kvp in _robbedNPCs)
                {
                    if ((DateTime.Now - kvp.Value).TotalMinutes > 5.0)
                    {
                        cooldownsToRemove.Add(kvp.Key);
                    }
                }
                foreach (int key in cooldownsToRemove)
                {
                    _robbedNPCs.Remove(key);
                }

                List<int> refsToRemove = new List<int>();
                foreach (var kvp in _complyingPedReferences)
                {
                    if (!_complyingNPCs.ContainsKey(kvp.Key))
                    {
                        refsToRemove.Add(kvp.Key);
                    }
                }
                foreach (int key in refsToRemove)
                {
                    _complyingPedReferences.Remove(key);
                }
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in CleanupOldData", ex);
            }
        }

        private string GetZoneName(Vector3 position)
        {
            try
            {
                return Function.Call<string>(Hash.GET_NAME_OF_ZONE, position.X, position.Y, position.Z);
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetStreetName(Vector3 position)
        {
            try
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
            catch
            {
                return "Unknown Street";
            }
        }

        public void Shutdown()
        {
            try
            {
                foreach (var kvp in _complyingPedReferences)
                {
                    if (kvp.Value != null && kvp.Value.Exists())
                    {
                        UnblockNPC(kvp.Value);
                    }
                }

                _npcWallets.Clear();
                _robbedNPCs.Clear();
                _complyingNPCs.Clear();
                _complyingPedReferences.Clear();
                _currentTarget = null;

                CrimeLogger.Log("🔄 RobberyInteraction desligado");
            }
            catch (Exception ex)
            {
                CrimeLogger.LogError("Error in Shutdown", ex);
            }
        }
    }
}