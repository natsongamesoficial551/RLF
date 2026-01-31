using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Agendador realista de denúncias de crimes.
    /// Testemunhas não reportam instantaneamente - existe delay, probabilidade de falha e imprecisão.
    /// </summary>
    public class CrimeReportScheduler
    {
        private class PendingReport
        {
            public CrimeRecord Crime { get; set; }
            public NPCReactionProfile Witness { get; set; }
            public DateTime ScheduledTime { get; set; }
            public bool IsProcessed { get; set; }
        }

        private readonly CrimeSystem _crimeSystem;
        private readonly List<PendingReport> _pendingReports;
        private readonly Dictionary<int, NPCReactionProfile> _witnessProfiles;

        private const float UPDATE_INTERVAL = 1.0f;
        private float _updateTimer;

        public bool IsEnabled { get; set; }
        public int PendingReportCount => _pendingReports.Count;

        public CrimeReportScheduler(CrimeSystem crimeSystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));
            _pendingReports = new List<PendingReport>();
            _witnessProfiles = new Dictionary<int, NPCReactionProfile>();
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

            ProcessPendingReports();
            CleanupOldReports();
        }

        private void OnCrimeCommitted(CrimeRecord crime)
        {
            if (crime == null) return;
            if (!IsEnabled) return;
            if (!crime.WasWitnessed()) return;

            ScheduleReportsForCrime(crime);
        }

        private void ScheduleReportsForCrime(CrimeRecord crime)
        {
            if (crime == null) return;

            Vector3 crimeLocation = new Vector3(crime.LocationX, crime.LocationY, crime.LocationZ);
            Ped[] nearbyPeds = World.GetNearbyPeds(crimeLocation, 50f);

            if (nearbyPeds == null || nearbyPeds.Length == 0) return;

            foreach (Ped ped in nearbyPeds)
            {
                if (!IsValidReporter(ped)) continue;

                NPCReactionProfile profile = GetOrCreateProfile(ped);
                if (profile == null || !profile.IsValid()) continue;

                float reportProbability = profile.GetReportProbability();
                Random rng = new Random(ped.Handle + (int)DateTime.Now.Ticks);

                if (rng.NextDouble() < reportProbability)
                {
                    ScheduleReport(crime, profile, rng);
                }
            }
        }

        private void ScheduleReport(CrimeRecord crime, NPCReactionProfile profile, Random rng)
        {
            if (crime == null || profile == null) return;

            float delaySeconds = GetReportDelay(profile, rng);

            PendingReport report = new PendingReport
            {
                Crime = crime,
                Witness = profile,
                ScheduledTime = DateTime.Now.AddSeconds(delaySeconds),
                IsProcessed = false
            };

            _pendingReports.Add(report);
        }

        private float GetReportDelay(NPCReactionProfile profile, Random rng)
        {
            if (profile == null) return 30f;

            float baseDelay = 15f;

            if (profile.IsCop)
            {
                return (float)(rng.NextDouble() * 5f + 2f);
            }

            baseDelay += profile.Fear * 20f;
            baseDelay -= profile.Courage * 10f;
            baseDelay -= profile.Intelligence * 5f;

            if (profile.IsInGang)
            {
                baseDelay += 30f;
            }

            float variance = (float)(rng.NextDouble() * 15f - 7.5f);
            baseDelay += variance;

            return Math.Max(5f, Math.Min(baseDelay, 120f));
        }

        private void ProcessPendingReports()
        {
            DateTime now = DateTime.Now;

            for (int i = _pendingReports.Count - 1; i >= 0; i--)
            {
                PendingReport report = _pendingReports[i];
                
                if (report == null || report.IsProcessed)
                {
                    _pendingReports.RemoveAt(i);
                    continue;
                }

                if (now >= report.ScheduledTime)
                {
                    ProcessReport(report);
                    report.IsProcessed = true;
                    _pendingReports.RemoveAt(i);
                }
            }
        }

        private void ProcessReport(PendingReport report)
        {
            if (report == null || report.Crime == null) return;
            if (report.Witness == null || !report.Witness.IsValid()) return;

            if (ShouldReportFail(report.Witness))
            {
                return;
            }

            _crimeSystem.MarkCrimeAsReported(report.Crime);

            if (report.Witness.IsCop)
            {
                SpawnPoliceResponse(report.Crime);
            }
        }

        private bool ShouldReportFail(NPCReactionProfile profile)
        {
            if (profile == null || !profile.IsValid()) return true;
            if (profile.IsCop) return false;

            Random rng = new Random();

            if (profile.Ped.IsDead) return true;
            if (!profile.Ped.IsAlive) return true;

            float failChance = 0.1f;
            failChance += profile.Fear * 0.15f;
            failChance -= profile.Intelligence * 0.1f;

            if (profile.IsInGang) failChance += 0.6f;

            float distanceFromPlayer = profile.Ped.Position.DistanceTo(Game.Player.Character.Position);
            if (distanceFromPlayer > 200f)
            {
                failChance *= 0.5f;
            }

            return rng.NextDouble() < failChance;
        }

        private void SpawnPoliceResponse(CrimeRecord crime)
        {
            if (crime == null) return;
        }

        private bool IsValidReporter(Ped ped)
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

        private void CleanupOldReports()
        {
            DateTime cutoff = DateTime.Now.AddMinutes(-5);

            for (int i = _pendingReports.Count - 1; i >= 0; i--)
            {
                PendingReport report = _pendingReports[i];
                
                if (report == null || report.ScheduledTime < cutoff)
                {
                    _pendingReports.RemoveAt(i);
                }
            }

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

        public void CancelAllReports()
        {
            _pendingReports.Clear();
        }

        public void Shutdown()
        {
            CrimeEvents.OnCrimeCommitted -= OnCrimeCommitted;
            _pendingReports.Clear();
            _witnessProfiles.Clear();
        }
    }
}
