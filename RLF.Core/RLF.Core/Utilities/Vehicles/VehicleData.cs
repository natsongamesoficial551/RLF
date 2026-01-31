using System;

namespace RLF.Core.Vehicles
{
    public sealed class VehicleData
    {
        public Guid Id { get; set; }

        public int Model { get; set; }
        public string Plate { get; set; }

        public int PrimaryColor { get; set; }
        public int SecondaryColor { get; set; }

        // 🔽 Persistência de garagem física
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float Heading { get; set; }

        public VehicleState State { get; set; }
    }
}
