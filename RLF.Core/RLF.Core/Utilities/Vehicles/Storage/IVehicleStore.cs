using System.Collections.Generic;

namespace RLF.Core.Vehicles.Storage
{
    /// <summary>
    /// Interface de persistência de veículos.
    /// </summary>
    public interface IVehicleStore
    {
        IEnumerable<VehicleData> Load();
        void Save(IEnumerable<VehicleData> vehicles);
    }
}
