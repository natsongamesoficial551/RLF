using RLF.Core.Configuration;
using System;
using System.Collections.Generic;

namespace RLF.Core.Vehicles.Storage
{
    public sealed class IniVehicleStore : IVehicleStore
    {
        private readonly IniReader _ini;

        public IniVehicleStore(string path)
        {
            _ini = new IniReader(path);
            _ini.Load();
        }

        public IEnumerable<VehicleData> Load()
        {
            var list = new List<VehicleData>();

            int count = _ini.GetInt("Vehicles", "Count", 0);

            for (int i = 0; i < count; i++)
            {
                string section = $"Vehicle_{i}";

                var data = new VehicleData
                {
                    Id = Guid.Parse(_ini.GetString(section, "Id", Guid.NewGuid().ToString())),
                    Model = _ini.GetInt(section, "Model", 0),
                    Plate = _ini.GetString(section, "Plate", ""),
                    PrimaryColor = _ini.GetInt(section, "PrimaryColor", 0),
                    SecondaryColor = _ini.GetInt(section, "SecondaryColor", 0),
                    PosX = _ini.GetFloat(section, "PosX", 0f),
                    PosY = _ini.GetFloat(section, "PosY", 0f),
                    PosZ = _ini.GetFloat(section, "PosZ", 0f),
                    Heading = _ini.GetFloat(section, "Heading", 0f),
                    State = (VehicleState)_ini.GetInt(section, "State", 0)
                };

                list.Add(data);
            }

            return list;
        }

        public void Save(IEnumerable<VehicleData> vehicles)
        {
            int index = 0;

            foreach (var v in vehicles)
            {
                string section = $"Vehicle_{index}";

                _ini.SetString(section, "Id", v.Id.ToString());
                _ini.SetInt(section, "Model", v.Model);
                _ini.SetString(section, "Plate", v.Plate ?? "");
                _ini.SetInt(section, "PrimaryColor", v.PrimaryColor);
                _ini.SetInt(section, "SecondaryColor", v.SecondaryColor);
                _ini.SetFloat(section, "PosX", v.PosX);
                _ini.SetFloat(section, "PosY", v.PosY);
                _ini.SetFloat(section, "PosZ", v.PosZ);
                _ini.SetFloat(section, "Heading", v.Heading);
                _ini.SetInt(section, "State", (int)v.State);

                index++;
            }

            _ini.SetInt("Vehicles", "Count", index);
            _ini.Save();
        }
    }
}
