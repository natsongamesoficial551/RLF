using System;
using System.Collections.Generic;

namespace RLF.Core.Crime
{
    /// <summary>
    /// Evidências e provas relacionadas a um crime.
    /// Acumuladas durante e após o crime, impactam investigação e Heat.
    /// </summary>
    public class CrimeEvidence
    {
        public List<string> Witnesses { get; private set; }
        public List<string> PhysicalEvidence { get; private set; }
        public List<string> CameraFootage { get; private set; }
        public bool HasDNA { get; set; }
        public bool HasFingerprints { get; set; }
        public bool HasWeaponSerial { get; set; }
        public float TotalEvidenceStrength { get; private set; }

        public CrimeEvidence()
        {
            Witnesses = new List<string>();
            PhysicalEvidence = new List<string>();
            CameraFootage = new List<string>();
            HasDNA = false;
            HasFingerprints = false;
            HasWeaponSerial = false;
            TotalEvidenceStrength = 0f;
        }

        public void AddWitness(string witnessId)
        {
            if (!string.IsNullOrEmpty(witnessId) && !Witnesses.Contains(witnessId))
            {
                Witnesses.Add(witnessId);
                RecalculateStrength();
            }
        }

        public void AddPhysicalEvidence(string evidenceDescription)
        {
            if (!string.IsNullOrEmpty(evidenceDescription))
            {
                PhysicalEvidence.Add(evidenceDescription);
                RecalculateStrength();
            }
        }

        public void AddCameraFootage(string cameraId)
        {
            if (!string.IsNullOrEmpty(cameraId) && !CameraFootage.Contains(cameraId))
            {
                CameraFootage.Add(cameraId);
                RecalculateStrength();
            }
        }

        public void SetForensicEvidence(bool dna, bool fingerprints, bool weaponSerial)
        {
            HasDNA = dna;
            HasFingerprints = fingerprints;
            HasWeaponSerial = weaponSerial;
            RecalculateStrength();
        }

        private void RecalculateStrength()
        {
            float strength = 0f;

            strength += Witnesses.Count * 0.15f;
            strength += PhysicalEvidence.Count * 0.10f;
            strength += CameraFootage.Count * 0.20f;

            if (HasDNA) strength += 0.30f;
            if (HasFingerprints) strength += 0.25f;
            if (HasWeaponSerial) strength += 0.20f;

            TotalEvidenceStrength = Math.Max(0f, Math.Min(strength, 1f));
        }

        public bool HasStrongEvidence()
        {
            return TotalEvidenceStrength >= 0.6f;
        }

        public bool HasAnyWitnesses()
        {
            return Witnesses.Count > 0;
        }

        public bool HasVideoEvidence()
        {
            return CameraFootage.Count > 0;
        }

        public int GetWitnessCount()
        {
            return Witnesses.Count;
        }

        public void Clear()
        {
            Witnesses.Clear();
            PhysicalEvidence.Clear();
            CameraFootage.Clear();
            HasDNA = false;
            HasFingerprints = false;
            HasWeaponSerial = false;
            TotalEvidenceStrength = 0f;
        }
    }
}
