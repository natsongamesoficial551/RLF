using System;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Crime
{
    /// <summary>
    /// Caso criminal completo que agrupa múltiplos CrimeRecords relacionados.
    /// Representa uma investigação ou série de crimes conectados.
    /// </summary>
    public class CrimeCase
    {
        public Guid CaseId { get; private set; }
        public string CaseNumber { get; set; }
        public DateTime OpenedAt { get; private set; }
        public DateTime LastActivityAt { get; private set; }
        public bool IsActive { get; private set; }
        public CrimeSeverity MaxSeverity { get; private set; }
        public List<CrimeRecord> Crimes { get; private set; }
        public SuspectDescription PrimarySuspect { get; private set; }
        public float TotalHeatGenerated { get; private set; }
        public bool IsEligibleForArrest { get; private set; }

        public CrimeCase()
        {
            CaseId = Guid.NewGuid();
            CaseNumber = GenerateCaseNumber();
            OpenedAt = DateTime.Now;
            LastActivityAt = DateTime.Now;
            IsActive = true;
            MaxSeverity = CrimeSeverity.None;
            Crimes = new List<CrimeRecord>();
            PrimarySuspect = new SuspectDescription();
            TotalHeatGenerated = 0f;
            IsEligibleForArrest = false;
        }

        public void AddCrime(CrimeRecord crime)
        {
            if (crime == null) return;

            Crimes.Add(crime);
            LastActivityAt = DateTime.Now;

            if (crime.Severity > MaxSeverity)
            {
                MaxSeverity = crime.Severity;
            }

            if (crime.IsEligibleForArrest())
            {
                IsEligibleForArrest = true;
            }

            UpdateSuspectInformation(crime.Suspect);
            RecalculateHeat();
        }

        public void Close()
        {
            IsActive = false;
        }

        public void Reopen()
        {
            IsActive = true;
            LastActivityAt = DateTime.Now;
        }

        public int GetCrimeCount()
        {
            return Crimes.Count;
        }

        public int GetCrimeCountByType(CrimeType type)
        {
            return Crimes.Count(c => c.Type == type);
        }

        public int GetCrimeCountBySeverity(CrimeSeverity severity)
        {
            return Crimes.Count(c => c.Severity == severity);
        }

        public bool HasCrimeType(CrimeType type)
        {
            return Crimes.Any(c => c.Type == type);
        }

        public bool HasViolentCrimes()
        {
            return Crimes.Any(c => c.HasFlag(CrimeFlags.Violent));
        }

        public bool HasWeaponCrimes()
        {
            return Crimes.Any(c => c.HasFlag(CrimeFlags.WeaponUsed));
        }

        public List<CrimeRecord> GetRecentCrimes(TimeSpan timeWindow)
        {
            DateTime cutoff = DateTime.Now - timeWindow;
            return Crimes.Where(c => c.Timestamp >= cutoff).ToList();
        }

        public CrimeRecord GetMostRecentCrime()
        {
            return Crimes.OrderByDescending(c => c.Timestamp).FirstOrDefault();
        }

        public CrimeRecord GetMostSevereCrime()
        {
            return Crimes.OrderByDescending(c => c.Severity).FirstOrDefault();
        }

        public TimeSpan TimeSinceLastActivity()
        {
            return DateTime.Now - LastActivityAt;
        }

        public TimeSpan CaseDuration()
        {
            return DateTime.Now - OpenedAt;
        }

        public float GetTotalMonetaryValue()
        {
            return Crimes.Sum(c => c.MonetaryValue);
        }

        private void UpdateSuspectInformation(SuspectDescription newInfo)
        {
            if (newInfo == null) return;

            if (!string.IsNullOrEmpty(newInfo.CharacterName))
            {
                PrimarySuspect.UpdateIdentification(
                    newInfo.CharacterName,
                    newInfo.IdentificationConfidence
                );
            }

            if (newInfo.HasVehicleInfo())
            {
                PrimarySuspect.UpdateVehicle(
                    newInfo.VehicleModel,
                    newInfo.VehiclePlate
                );
            }

            if (!string.IsNullOrEmpty(newInfo.LastSeenLocation))
            {
                PrimarySuspect.UpdateLastSeen(newInfo.LastSeenLocation);
            }

            if (!string.IsNullOrEmpty(newInfo.ClothingDescription))
            {
                PrimarySuspect.ClothingDescription = newInfo.ClothingDescription;
            }
        }

        private void RecalculateHeat()
        {
            float totalHeat = 0f;

            foreach (var crime in Crimes)
            {
                float crimeHeat = crime.GetHeatContribution();

                TimeSpan age = crime.TimeSinceCrime();
                float decay = CalculateHeatDecay(age);

                totalHeat += crimeHeat * decay;
            }

            if (Crimes.Count > 3)
            {
                float recidivismMultiplier = 1f + (Crimes.Count - 3) * 0.1f;
                totalHeat *= Math.Min(recidivismMultiplier, 2f);
            }

            TotalHeatGenerated = Math.Max(0f, Math.Min(totalHeat, 1f));
        }

        private float CalculateHeatDecay(TimeSpan age)
        {
            double hours = age.TotalHours;

            if (hours < 1) return 1f;
            if (hours < 6) return 0.9f;
            if (hours < 24) return 0.7f;
            if (hours < 72) return 0.5f;
            if (hours < 168) return 0.3f;

            return 0.1f;
        }

        private string GenerateCaseNumber()
        {
            DateTime now = DateTime.Now;
            string year = now.Year.ToString().Substring(2);
            string month = now.Month.ToString("D2");
            string day = now.Day.ToString("D2");
            string random = new Random().Next(1000, 9999).ToString();

            return $"RLF{year}{month}{day}-{random}";
        }
    }
}
