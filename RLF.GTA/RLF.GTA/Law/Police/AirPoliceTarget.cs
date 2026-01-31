using GTA;

namespace RLF.GTA.Law.Police
{
    public sealed class AirPoliceTarget
    {
        public Ped Ped { get; }
        public Vehicle Aircraft { get; }
        public float InitialAltitude { get; }

        public AirPoliceTarget(Ped ped, Vehicle aircraft, float altitude)
        {
            Ped = ped;
            Aircraft = aircraft;
            InitialAltitude = altitude;
        }

        public bool IsValid()
        {
            return Ped != null && Ped.Exists() &&
                   Aircraft != null && Aircraft.Exists();
        }
    }
}