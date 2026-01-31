using System;

namespace RLF.Core.Crime
{
    /// <summary>
    /// Descrição de um suspeito criminal.
    /// Armazena informações de identificação coletadas durante ou após o crime.
    /// </summary>
    public class SuspectDescription
    {
        public string CharacterName { get; set; }
        public int? ModelHash { get; set; }
        public string ClothingDescription { get; set; }
        public string VehicleModel { get; set; }
        public string VehiclePlate { get; set; }
        public float Height { get; set; }
        public bool IsIdentified { get; set; }
        public float IdentificationConfidence { get; set; }
        public DateTime LastSeenTime { get; set; }
        public string LastSeenLocation { get; set; }

        public SuspectDescription()
        {
            CharacterName = string.Empty;
            ClothingDescription = string.Empty;
            VehicleModel = string.Empty;
            VehiclePlate = string.Empty;
            LastSeenLocation = string.Empty;
            IsIdentified = false;
            IdentificationConfidence = 0f;
            LastSeenTime = DateTime.Now;
        }

        public void UpdateIdentification(string characterName, float confidence)
        {
            if (confidence > IdentificationConfidence)
            {
                CharacterName = characterName;
                IdentificationConfidence = Math.Max(0f, Math.Min(confidence, 1f));
                IsIdentified = IdentificationConfidence >= 0.7f;
            }
        }

        public void UpdateVehicle(string model, string plate)
        {
            VehicleModel = model ?? string.Empty;
            VehiclePlate = plate ?? string.Empty;
        }

        public void UpdateLastSeen(string location)
        {
            LastSeenLocation = location ?? string.Empty;
            LastSeenTime = DateTime.Now;
        }

        public bool HasVehicleInfo()
        {
            return !string.IsNullOrEmpty(VehicleModel) || !string.IsNullOrEmpty(VehiclePlate);
        }

        public TimeSpan TimeSinceLastSeen()
        {
            return DateTime.Now - LastSeenTime;
        }
    }
}
