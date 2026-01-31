using GTA;

namespace RLF.GTA.Law.Police
{
    public sealed class PoliceTarget
    {
        public Ped Ped { get; }
        public Vehicle Vehicle { get; }

        public PoliceTarget(Ped ped, Vehicle vehicle)
        {
            Ped = ped;
            Vehicle = vehicle;
        }

        public bool IsValid()
        {
            return Ped != null && Ped.Exists() &&
                   Vehicle != null && Vehicle.Exists();
        }
    }
}