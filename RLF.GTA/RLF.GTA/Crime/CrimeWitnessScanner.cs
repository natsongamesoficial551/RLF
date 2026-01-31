using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Sistema de escaneamento de testemunhas de crimes.
    /// Detecta NPCs que presenciam crimes e registra como testemunhas no CrimeSystem.
    /// </summary>
    public class CrimeWitnessScanner
    {
        private readonly CrimeSystem _crimeSystem;
        private readonly Dictionary<int, NPCReactionProfile> _witnessProfiles;

        private const float WITNESS_RADIUS = 50f;
        private const float MAX_WITNESS_DISTANCE = 100f;
        private const float SCAN_INTERVAL = 0.5f;
        private float _scanTimer;

        public bool IsEnabled { get; set; }

        public CrimeWitnessScanner(CrimeSystem crimeSystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));
            _witnessProfiles = new Dictionary<int, NPCReactionProfile>();
            _scanTimer = 0f;
            IsEnabled = true;

            CrimeEvents.OnCrimeCommitted += OnCrimeCommitted;
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _scanTimer += deltaTime;
            if (_scanTimer < SCAN_INTERVAL) return;
            _scanTimer = 0f;

            CleanupInvalidProfiles();
        }

        private void OnCrimeCommitted(CrimeRecord crime)
        {
            if (crime == null) return;
            if (!IsEnabled) return;

            Vector3 crimeLocation = new Vector3(crime.LocationX, crime.LocationY, crime.LocationZ);

            ScanForWitnesses(crime, crimeLocation);
        }

        private void ScanForWitnesses(CrimeRecord crime, Vector3 location)
        {
            Ped[] nearbyPeds = World.GetNearbyPeds(location, WITNESS_RADIUS);
            if (nearbyPeds == null || nearbyPeds.Length == 0) return;

            foreach (Ped ped in nearbyPeds)
            {
                if (!IsValidWitness(ped)) continue;

                float distance = ped.Position.DistanceTo(location);
                if (distance > MAX_WITNESS_DISTANCE) continue;

                if (HasLineOfSight(ped, location))
                {
                    ProcessWitness(ped, crime, distance);
                }
            }
        }

        private bool IsValidWitness(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;
            if (!ped.IsAlive) return false;
            if (ped.IsPlayer) return false;
            if (ped.IsInVehicle() && !IsDriver(ped)) return false;

            return true;
        }

        private bool IsDriver(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;
            if (!ped.IsInVehicle()) return false;

            Vehicle vehicle = ped.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists()) return false;

            return ped.SeatIndex == VehicleSeat.Driver;
        }

        private bool HasLineOfSight(Ped ped, Vector3 location)
        {
            if (ped == null || !ped.Exists()) return false;

            // Verificação simples baseada em distância
            float distance = ped.Position.DistanceTo(location);

            // Quanto mais perto, maior a chance de ter visto
            if (distance < 20f) return true;  // Muito perto, com certeza viu
            if (distance > 70f) return false; // Muito longe, não viu

            // Distância média - 50% de chance baseado em obstruções aleatórias
            Random rng = new Random(ped.Handle + (int)distance);
            return rng.NextDouble() > 0.3; // 70% de chance de ter linha de visão
        }

        private void ProcessWitness(Ped ped, CrimeRecord crime, float distance)
        {
            if (ped == null || !ped.Exists()) return;
            if (crime == null) return;

            NPCReactionProfile profile = GetOrCreateProfile(ped);
            if (profile == null || !profile.IsValid()) return;

            profile.DistanceFromCrime = distance;

            string witnessId = $"ped_{ped.Handle}";
            _crimeSystem.MarkCrimeAsWitnessed(crime, witnessId);

            float witnessChance = CalculateWitnessChance(profile, distance);
            Random rng = new Random();

            if (rng.NextDouble() < witnessChance)
            {
                crime.AddFlag(CrimeFlags.Witnessed);
                RegisterWitnessEvidence(crime, profile);
            }
        }

        private NPCReactionProfile GetOrCreateProfile(Ped ped)
        {
            if (ped == null || !ped.Exists()) return null;

            int handle = ped.Handle;

            if (_witnessProfiles.ContainsKey(handle))
            {
                NPCReactionProfile existing = _witnessProfiles[handle];
                if (existing != null && existing.IsValid())
                {
                    return existing;
                }
                else
                {
                    _witnessProfiles.Remove(handle);
                }
            }

            NPCReactionProfile newProfile = new NPCReactionProfile(ped);
            if (newProfile.IsValid())
            {
                _witnessProfiles[handle] = newProfile;
                return newProfile;
            }

            return null;
        }

        private float CalculateWitnessChance(NPCReactionProfile profile, float distance)
        {
            if (profile == null || !profile.IsValid()) return 0f;

            float baseChance = 0.8f;

            if (distance > 30f) baseChance *= 0.7f;
            if (distance > 50f) baseChance *= 0.5f;
            if (distance > 70f) baseChance *= 0.3f;

            baseChance += profile.Intelligence * 0.1f;
            baseChance -= profile.Fear * 0.1f;

            if (profile.IsCop) baseChance = 1f;
            if (profile.IsInGang) baseChance *= 0.6f;

            return Math.Max(0f, Math.Min(baseChance, 1f));
        }

        private void RegisterWitnessEvidence(CrimeRecord crime, NPCReactionProfile profile)
        {
            if (crime == null || profile == null) return;

            crime.Evidence.AddWitness($"ped_{profile.Ped.Handle}");

            float identificationChance = CalculateIdentificationChance(profile);
            Random rng = new Random();

            if (rng.NextDouble() < identificationChance)
            {
                Ped player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    string description = GetPlayerDescription(player);
                    crime.Suspect.ClothingDescription = description;

                    float confidence = identificationChance;
                    _crimeSystem.IdentifySuspect(crime, "Player", confidence);

                    if (player.IsInVehicle())
                    {
                        Vehicle vehicle = player.CurrentVehicle;
                        if (vehicle != null && vehicle.Exists())
                        {
                            string model = GetVehicleDisplayName(vehicle);
                            string plate = GetVehiclePlate(vehicle);
                            _crimeSystem.IdentifySuspectVehicle(crime, model, plate);
                        }
                    }
                }
            }
        }

        private float CalculateIdentificationChance(NPCReactionProfile profile)
        {
            if (profile == null || !profile.IsValid()) return 0f;

            float baseChance = 0.5f;

            baseChance += profile.Intelligence * 0.3f;
            baseChance -= profile.Fear * 0.2f;
            baseChance -= (profile.DistanceFromCrime / MAX_WITNESS_DISTANCE) * 0.3f;

            if (profile.IsCop) baseChance = 0.95f;

            return Math.Max(0f, Math.Min(baseChance, 1f));
        }

        private string GetPlayerDescription(Ped player)
        {
            if (player == null || !player.Exists()) return "Unknown";

            List<string> traits = new List<string>();

            if (player.Gender == Gender.Male) traits.Add("Male");
            else traits.Add("Female");

            PedHash modelHash = (PedHash)player.Model.Hash;
            traits.Add(modelHash.ToString());

            if (player.Weapons.Current != null &&
                player.Weapons.Current.Hash != WeaponHash.Unarmed)
            {
                traits.Add($"Armed with {player.Weapons.Current.Hash}");
            }

            return string.Join(", ", traits);
        }

        private string GetVehicleDisplayName(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return "Unknown";

            // Solução simples: apenas retorna o display name do modelo
            string displayName = Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, vehicle.Model.Hash);
            return displayName ?? vehicle.Model.Hash.ToString();
        }

        private string GetVehiclePlate(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return "UNKNOWN";
            return Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, vehicle.Handle);
        }

        private void CleanupInvalidProfiles()
        {
            List<int> toRemove = new List<int>();

            foreach (var kvp in _witnessProfiles)
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
                _witnessProfiles.Remove(key);
            }
        }

        public int GetActiveWitnessCount()
        {
            return _witnessProfiles.Count(kvp => kvp.Value != null && kvp.Value.IsValid());
        }

        public void Shutdown()
        {
            CrimeEvents.OnCrimeCommitted -= OnCrimeCommitted;
            _witnessProfiles.Clear();
        }
    }
}
