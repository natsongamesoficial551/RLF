using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;
using System;
using System.Collections.Generic;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Controlador de reações de NPCs a crimes testemunhados.
    /// Aplica comportamentos realistas e variáveis baseados em perfil comportamental.
    /// </summary>
    public class NPCReactionController
    {
        private class NPCReaction
        {
            public Ped Ped { get; set; }
            public NPCReactionProfile Profile { get; set; }
            public ReactionType Type { get; set; }
            public DateTime StartedAt { get; set; }
            public bool IsComplete { get; set; }
        }

        private enum ReactionType
        {
            None,
            Flee,
            FightBack,
            Comply,
            CallForHelp,
            Freeze,
            VehicleFlee
        }

        private readonly CrimeSystem _crimeSystem;
        private readonly Dictionary<int, NPCReaction> _activeReactions;
        private readonly Dictionary<int, NPCReactionProfile> _profiles;

        private const float REACTION_RADIUS = 30f;
        private const float UPDATE_INTERVAL = 0.2f;
        private float _updateTimer;

        public bool IsEnabled { get; set; }
        public int ActiveReactionCount => _activeReactions.Count;

        public NPCReactionController(CrimeSystem crimeSystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));
            _activeReactions = new Dictionary<int, NPCReaction>();
            _profiles = new Dictionary<int, NPCReactionProfile>();
            _updateTimer = 0f;
            IsEnabled = true;

            CrimeEvents.OnCrimeCommitted -= OnCrimeCommitted;
            CrimeEvents.OnCrimeCommitted += OnCrimeCommitted;
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _updateTimer += deltaTime;
            if (_updateTimer < UPDATE_INTERVAL) return;
            _updateTimer = 0f;

            UpdateActiveReactions();
            CleanupInvalidReactions();
        }

        private void OnCrimeCommitted(CrimeRecord crime)
        {
            if (crime == null) return;
            if (!IsEnabled) return;

            Vector3 crimeLocation = new Vector3(crime.LocationX, crime.LocationY, crime.LocationZ);
            ProcessNearbyNPCReactions(crime, crimeLocation);
        }

        private void ProcessNearbyNPCReactions(CrimeRecord crime, Vector3 location)
        {
            Ped[] nearbyPeds = World.GetNearbyPeds(location, REACTION_RADIUS);
            if (nearbyPeds == null || nearbyPeds.Length == 0) return;

            foreach (Ped ped in nearbyPeds)
            {
                if (!IsValidForReaction(ped)) continue;
                if (_activeReactions.ContainsKey(ped.Handle)) continue;

                NPCReactionProfile profile = GetOrCreateProfile(ped);
                if (profile == null || !profile.IsValid()) continue;

                ApplyReaction(ped, profile, crime);
            }
        }

        private void ApplyReaction(Ped ped, NPCReactionProfile profile, CrimeRecord crime)
        {
            if (ped == null || !ped.Exists()) return;
            if (profile == null || !profile.IsValid()) return;

            ReactionType reaction = DetermineReaction(profile, crime);
            if (reaction == ReactionType.None) return;

            NPCReaction npcReaction = new NPCReaction
            {
                Ped = ped,
                Profile = profile,
                Type = reaction,
                StartedAt = DateTime.Now,
                IsComplete = false
            };

            _activeReactions[ped.Handle] = npcReaction;
            ExecuteReaction(npcReaction);
        }

        private ReactionType DetermineReaction(NPCReactionProfile profile, CrimeRecord crime)
        {
            if (profile == null || !profile.IsValid()) return ReactionType.None;

            Random rng = new Random(profile.Ped.Handle + (int)DateTime.Now.Ticks);

            if (profile.IsCop)
            {
                return ReactionType.FightBack;
            }

            bool isViolentCrime = crime.HasFlag(CrimeFlags.Violent) ||
                                 crime.HasFlag(CrimeFlags.WeaponUsed);

            // NOVAS PROBABILIDADES BALANCEADAS
            float surrenderChance = 0.40f; // 40% - Render-se (PRINCIPAL)
            float fleeChance = 0.30f;      // 30% - Fugir
            float fightChance = 0.15f;     // 15% - Lutar
            float callChance = 0.10f;      // 10% - Ligar 911
            float freezeChance = 0.05f;    // 5% - Congelar

            // Ajustes baseados em perfil
            surrenderChance += profile.Fear * 0.2f;
            surrenderChance += profile.Intelligence * 0.1f;

            fleeChance -= profile.Courage * 0.1f;
            fightChance += profile.Aggression * 0.15f;

            if (isViolentCrime)
            {
                surrenderChance += 0.15f; // Mais chance de render em crime violento
                fleeChance += 0.10f;
                fightChance -= 0.10f;
            }

            if (profile.IsInGang)
            {
                surrenderChance *= 0.3f; // Gangues rendem menos
                fightChance *= 2.0f;
            }

            if (profile.IsArmed)
            {
                surrenderChance *= 0.6f;
                fightChance *= 1.5f;
            }

            // Verifica se está em veículo
            if (profile.Ped.IsInVehicle() && profile.Ped.SeatIndex == VehicleSeat.Driver)
            {
                if (rng.NextDouble() < 0.7)
                {
                    return ReactionType.VehicleFlee;
                }
            }

            // Normaliza para somar 1.0
            float total = surrenderChance + fleeChance + fightChance + callChance + freezeChance;
            surrenderChance /= total;
            fleeChance /= total;
            fightChance /= total;
            callChance /= total;
            freezeChance /= total;

            // Rola o dado
            float roll = (float)rng.NextDouble();
            float cumulative = 0f;

            cumulative += surrenderChance;
            if (roll < cumulative) return ReactionType.Comply;

            cumulative += fleeChance;
            if (roll < cumulative) return ReactionType.Flee;

            cumulative += fightChance;
            if (roll < cumulative) return ReactionType.FightBack;

            cumulative += callChance;
            if (roll < cumulative) return ReactionType.CallForHelp;

            return ReactionType.Freeze;
        }

        private void ExecuteReaction(NPCReaction reaction)
        {
            if (reaction == null || reaction.Ped == null || !reaction.Ped.Exists()) return;

            Ped ped = reaction.Ped;
            Ped player = Game.Player.Character;

            if (player == null || !player.Exists()) return;

            switch (reaction.Type)
            {
                case ReactionType.Flee:
                    ExecuteFlee(ped, player);
                    break;

                case ReactionType.FightBack:
                    ExecuteFightBack(ped, player);
                    break;

                case ReactionType.Comply:
                    ExecuteComply(ped);
                    break;

                case ReactionType.CallForHelp:
                    ExecuteCallForHelp(ped);
                    break;

                case ReactionType.Freeze:
                    ExecuteFreeze(ped);
                    break;

                case ReactionType.VehicleFlee:
                    ExecuteVehicleFlee(ped, player);
                    break;
            }
        }

        private void ExecuteFlee(Ped ped, Ped player)
        {
            if (ped == null || !ped.Exists()) return;

            ped.Task.ClearAll();

            Vector3 fleeDirection = ped.Position - player.Position;
            fleeDirection.Normalize();
            Vector3 fleeTarget = ped.Position + fleeDirection * 100f;

            ped.Task.RunTo(fleeTarget);

            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped.Handle, 0, false);
            Function.Call(Hash.SET_PED_KEEP_TASK, ped.Handle, true);
        }

        private void ExecuteFightBack(Ped ped, Ped player)
        {
            if (ped == null || !ped.Exists()) return;

            // NOVA LÓGICA: Chance MUITO MENOR de ter arma
            // Só ganha arma se:
            // 1. For gangue (50% chance)
            // 2. For mafia/business suspicious (30% chance)
            // 3. Já tiver arma naturalmente

            if (!ped.Weapons.HasWeapon(WeaponHash.Pistol) &&
                !ped.Weapons.HasWeapon(WeaponHash.CombatPistol))
            {
                NPCReactionProfile profile = GetOrCreateProfile(ped);
                Random rng = new Random(ped.Handle);

                bool shouldGiveWeapon = false;

                if (profile != null)
                {
                    // Gangues: 50% de chance de ter arma
                    if (profile.IsInGang && rng.NextDouble() < 0.50)
                    {
                        shouldGiveWeapon = true;
                    }
                    // Mafiosos/Business suspeitos: 30% de chance
                    else if (IsSuspiciousBusiness(ped) && rng.NextDouble() < 0.30)
                    {
                        shouldGiveWeapon = true;
                    }
                    // Agressivos: 20% de chance
                    else if (profile.Aggression > 0.8f && rng.NextDouble() < 0.20)
                    {
                        shouldGiveWeapon = true;
                    }
                }

                if (shouldGiveWeapon)
                {
                    ped.Weapons.Give(WeaponHash.Pistol, 50, true, true);
                }
            }

            ped.Task.ClearAll();
            ped.Task.FightAgainst(player);

            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2);
        }

        private bool IsSuspiciousBusiness(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;

            // Detecta modelos de businessman/mafia
            PedHash[] suspiciousModels = new PedHash[]
            {
                PedHash.Business01AMM,
                PedHash.Business03AMY,
                PedHash.Business04AFY,
                // Adicione outros modelos suspeitos aqui
            };

            PedHash pedHash = (PedHash)ped.Model.Hash;
            foreach (var model in suspiciousModels)
            {
                if (pedHash == model)
                    return true;
            }

            return false;
        }

        private void ExecuteComply(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;

            // CRITICO: Se esta em veiculo, FORCA sair antes de se render
            if (ped.IsInVehicle())
            {
                Vehicle vehicle = ped.CurrentVehicle;

                // Limpa tasks primeiro
                ped.Task.ClearAll();

                // Forca sair do veiculo imediatamente
                ped.Task.LeaveVehicle(vehicle, LeaveVehicleFlags.None);

                // Espera um frame para garantir que saiu
                // O HandsUp sera aplicado no proximo update quando ja estiver fora
                return;
            }

            // Limpa todas as tasks primeiro
            ped.Task.ClearAll();

            // Aplica HandsUp com duração infinita
            ped.Task.HandsUp(-1);

            // Congela o NPC no lugar para não fugir
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped.Handle, true);

            // Desabilita fuga automática
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped.Handle, 0, false);

            // Força o NPC a não reagir a nada
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 17, false);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);

            // Congela fisicamente (ISSO RESOLVE O PROBLEMA DAS ASAS!)
            if (ped.Exists() && ped.IsAlive && !ped.IsRagdoll)
            {
                Function.Call(Hash.FREEZE_ENTITY_POSITION, ped.Handle, true);
            }

            // Remove reações de medo/dor
            Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, ped.Handle, true);
            Function.Call(Hash.STOP_PED_SPEAKING, ped.Handle, true);
        }

        private void ExecuteCallForHelp(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;

            ped.Task.ClearAll();
            ped.Task.StandStill(3000);
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped.Handle, true);

        }

        private void ExecuteFreeze(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;

            ped.Task.ClearAll();
            ped.Task.StandStill(5000);

            Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY,
                ped.Handle, Game.Player.Character.Handle, 2000);
        }

        private void ExecuteVehicleFlee(Ped ped, Ped player)
        {
            if (ped == null || !ped.Exists()) return;
            if (!ped.IsInVehicle()) return;

            Vehicle vehicle = ped.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists()) return;

            ped.Task.ClearAll();

            Vector3 fleeDirection = vehicle.Position - player.Position;
            fleeDirection.Normalize();
            Vector3 fleeTarget = vehicle.Position + fleeDirection * 200f;

            ped.Task.DriveTo(vehicle, fleeTarget, 10f, 50f);

            Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, ped.Handle, 786603);
        }

        private void UpdateActiveReactions()
        {
            List<int> toRemove = new List<int>();

            foreach (var kvp in _activeReactions)
            {
                NPCReaction reaction = kvp.Value;

                if (reaction == null || reaction.Ped == null || !reaction.Ped.Exists())
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                if (!reaction.Ped.IsAlive)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                TimeSpan elapsed = DateTime.Now - reaction.StartedAt;
                if (elapsed.TotalSeconds > 30.0)
                {
                    reaction.IsComplete = true;
                    toRemove.Add(kvp.Key);
                }
            }


            foreach (int key in toRemove)
            {
                if (_activeReactions.TryGetValue(key, out var reaction))
                {
                    if (reaction.Ped != null && reaction.Ped.Exists())
                    {
                        if (reaction.Type == ReactionType.Comply)
                        {
                            Function.Call(Hash.FREEZE_ENTITY_POSITION, reaction.Ped.Handle, false);
                            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, reaction.Ped.Handle, false);
                            reaction.Ped.Task.ClearAll();
                        }
                    }
                }

                _activeReactions.Remove(key);
            }

        }

        private void CleanupInvalidReactions()
        {
            List<int> toRemove = new List<int>();

            foreach (var kvp in _profiles)
            {
                if (kvp.Value == null || !kvp.Value.IsValid())
                {
                    toRemove.Add(kvp.Key);
                }
                else if (kvp.Value.TimeSinceCreation().TotalMinutes > 10)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (int key in toRemove)
            {
                _profiles.Remove(key);
            }
        }

        private bool IsValidForReaction(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;
            if (!ped.IsAlive) return false;
            if (ped.IsPlayer) return false;
            if (ped.IsDead) return false;

            return true;
        }

        private NPCReactionProfile GetOrCreateProfile(Ped ped)
        {
            if (ped == null || !ped.Exists()) return null;

            int handle = ped.Handle;

            if (_profiles.ContainsKey(handle))
            {
                NPCReactionProfile existing = _profiles[handle];
                if (existing != null && existing.IsValid())
                {
                    return existing;
                }
                else
                {
                    _profiles.Remove(handle);
                }
            }

            NPCReactionProfile newProfile = new NPCReactionProfile(ped);
            if (newProfile.IsValid())
            {
                _profiles[handle] = newProfile;
                return newProfile;
            }

            return null;
        }

        public void CancelAllReactions()
        {
            foreach (var kvp in _activeReactions)
            {
                NPCReaction reaction = kvp.Value;

                if (reaction?.Ped != null && reaction.Ped.Exists())
                {
                    // Descongela apenas se estava rendido
                    if (reaction.Type == ReactionType.Comply)
                    {
                        Function.Call(Hash.FREEZE_ENTITY_POSITION, reaction.Ped.Handle, false);
                        Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, reaction.Ped.Handle, false);
                        reaction.Ped.Task.ClearAll();
                    }
                }
            }

            _activeReactions.Clear();
        }


        public void Shutdown()
        {
            CrimeEvents.OnCrimeCommitted -= OnCrimeCommitted;
            _activeReactions.Clear();
            _profiles.Clear();
        }
    }
}
