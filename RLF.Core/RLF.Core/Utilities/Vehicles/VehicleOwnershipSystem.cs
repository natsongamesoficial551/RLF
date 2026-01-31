using System;
using System.Collections.Generic;
using System.Linq;
using RLF.Core.Vehicles.Storage;

namespace RLF.Core.Vehicles
{
    /// <summary>
    /// Sistema central de posse de veículos.
    /// </summary>
    public sealed class VehicleOwnershipSystem
    {
        private readonly List<VehicleData> _vehicles;
        private readonly IVehicleStore _store;

        public IReadOnlyList<VehicleData> Vehicles => _vehicles;

        public VehicleOwnershipSystem()
        {
            _store = new IniVehicleStore("scripts/RLF/vehicles.ini");
            _vehicles = new List<VehicleData>(_store.Load());
        }

        public void RegisterVehicle(VehicleData vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));

            if (_vehicles.Any(v => v.Id == vehicle.Id))
                return;

            _vehicles.Add(vehicle);
            Save();
        }

        public VehicleData GetById(Guid id)
        {
            return _vehicles.FirstOrDefault(v => v.Id == id);
        }

        public void Save()
        {
            _store.Save(_vehicles);
        }
    }
}
