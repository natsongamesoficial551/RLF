using System;

namespace RLF.Core.Crime
{
    /// <summary>
    /// Registro individual de um crime cometido.
    /// Representa uma ocorrência específica no tempo e espaço.
    /// </summary>
    public class CrimeRecord
    {
        public Guid CrimeId { get; private set; }
        public CrimeType Type { get; set; }
        public CrimeSeverity Severity { get; set; }
        public CrimeFlags Flags { get; set; }
        public DateTime Timestamp { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }
        public float LocationZ { get; set; }
        public string LocationName { get; set; }
        public string ZoneName { get; set; }
        public float MonetaryValue { get; set; }
        public SuspectDescription Suspect { get; set; }
        public CrimeEvidence Evidence { get; set; }

        public CrimeRecord(CrimeType type, CrimeSeverity severity)
        {
            CrimeId = Guid.NewGuid();
            Type = type;
            Severity = severity;
            Flags = CrimeFlags.None;
            Timestamp = DateTime.Now;
            LocationX = 0f;
            LocationY = 0f;
            LocationZ = 0f;
            LocationName = string.Empty;
            ZoneName = string.Empty;
            MonetaryValue = 0f;
            Suspect = new SuspectDescription();
            Evidence = new CrimeEvidence();
        }

        public void SetLocation(float x, float y, float z, string locationName, string zoneName)
        {
            LocationX = x;
            LocationY = y;
            LocationZ = z;
            LocationName = locationName ?? string.Empty;
            ZoneName = zoneName ?? string.Empty;
        }

        public void AddFlag(CrimeFlags flag)
        {
            Flags |= flag;
        }

        public void RemoveFlag(CrimeFlags flag)
        {
            Flags &= ~flag;
        }

        public bool HasFlag(CrimeFlags flag)
        {
            return (Flags & flag) == flag;
        }

        public bool IsEligibleForArrest()
        {
            return HasFlag(CrimeFlags.EligibleForArrest);
        }

        public bool WasWitnessed()
        {
            return HasFlag(CrimeFlags.Witnessed);
        }

        public bool WasReported()
        {
            return HasFlag(CrimeFlags.Reported);
        }

        public TimeSpan TimeSinceCrime()
        {
            return DateTime.Now - Timestamp;
        }

        public float GetHeatContribution()
        {
            float heat;

            switch (Severity)
            {
                case CrimeSeverity.Infraction:
                    heat = 0.05f;
                    break;
                case CrimeSeverity.Misdemeanor:
                    heat = 0.15f;
                    break;
                case CrimeSeverity.Felony:
                    heat = 0.40f;
                    break;
                case CrimeSeverity.ViolentFelony:
                    heat = 0.70f;
                    break;
                case CrimeSeverity.Capital:
                    heat = 1.00f;
                    break;
                default:
                    heat = 0f;
                    break;
            }

            if (HasFlag(CrimeFlags.Violent)) heat *= 1.3f;
            if (HasFlag(CrimeFlags.WeaponUsed)) heat *= 1.2f;
            if (HasFlag(CrimeFlags.VictimKilled)) heat *= 1.5f;
            if (HasFlag(CrimeFlags.Witnessed)) heat *= 1.1f;
            if (HasFlag(CrimeFlags.Reported)) heat *= 1.2f;

            return Math.Max(0f, Math.Min(heat, 1f));
        }
    }
}
