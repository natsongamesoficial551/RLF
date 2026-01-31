using GTA;
using RLF.GTA.Jobs.Uber.Ride;

namespace RLF.GTA.Jobs.Uber.Vehicle
{
    public static class UberVehicleCategory
    {
        public static RideCategory? GetCategory(global::GTA.Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists())
                return null;

            // Verifica se é carro
            if (!vehicle.Model.IsCar)
                return null;

            VehicleHash hash = (VehicleHash)vehicle.Model.Hash;

            // Uber Black (carros de luxo)
            if (IsUberBlack(hash))
                return RideCategory.UberBlack;

            // Uber Pool (carros com 4+ lugares)
            if (vehicle.PassengerCapacity >= 4)
                return RideCategory.UberPool;

            // Uber X (padrão)
            return RideCategory.UberX;
        }

        private static bool IsUberBlack(VehicleHash hash)
        {
            return hash == VehicleHash.Cognoscenti ||
                   hash == VehicleHash.Cognoscenti2 ||
                   hash == VehicleHash.Schafter2 ||
                   hash == VehicleHash.Schafter3 ||
                   hash == VehicleHash.Schafter4 ||
                   hash == VehicleHash.Schafter5 ||
                   hash == VehicleHash.Schafter6 ||
                   hash == VehicleHash.Superd ||        // ✅ CORRIGIDO: SuperDiamond → Superd
                   hash == VehicleHash.Tailgater ||
                   hash == VehicleHash.Windsor ||
                   hash == VehicleHash.Windsor2 ||
                   hash == VehicleHash.XLS ||
                   hash == VehicleHash.Baller ||
                   hash == VehicleHash.Baller2 ||
                   hash == VehicleHash.Baller3 ||
                   hash == VehicleHash.Baller4 ||
                   hash == VehicleHash.Baller5 ||
                   hash == VehicleHash.Baller6;
        }
    }
}