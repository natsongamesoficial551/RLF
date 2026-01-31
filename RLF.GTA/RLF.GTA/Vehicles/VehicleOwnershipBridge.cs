using RLF.Core;
using RLF.Core.Vehicles;

namespace RLF.GTA.Vehicles
{
    public static class VehicleOwnershipBridge
    {
        public static VehicleOwnershipSystem Current =>
            RLFCore.Instance.VehicleOwnership;
    }
}
