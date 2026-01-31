using GTA;
using System.Collections.Generic;

namespace RLF.GTA.Dealership
{
    public static class DealershipCatalog
    {
        public class VehicleEntry
        {
            public VehicleHash Model;
            public string Name;
            public int Price;
        }

        public static readonly Dictionary<string, List<VehicleEntry>> Categories =
            new Dictionary<string, List<VehicleEntry>>
            {
                {
                    "Carros Comuns",
                    new List<VehicleEntry>
                    {
                        new VehicleEntry { Name = "Blista", Model = VehicleHash.Blista, Price = 35000 },
                        new VehicleEntry { Name = "Panto", Model = VehicleHash.Panto, Price = 28000 },
                        new VehicleEntry { Name = "Asea", Model = VehicleHash.Asea, Price = 42000 }
                    }
                },
                {
                    "Carros Esportivos",
                    new List<VehicleEntry>
                    {
                        new VehicleEntry { Name = "Comet", Model = VehicleHash.Comet2, Price = 180000 },
                        new VehicleEntry { Name = "Feltzer", Model = VehicleHash.Feltzer2, Price = 220000 }
                    }
                },
                {
                    "SUVs",
                    new List<VehicleEntry>
                    {
                        new VehicleEntry { Name = "Baller", Model = VehicleHash.Baller, Price = 160000 },
                        new VehicleEntry { Name = "Cavalcade", Model = VehicleHash.Cavalcade, Price = 120000 }
                    }
                },
                {
                    "Super Carros",
                    new List<VehicleEntry>
                    {
                        new VehicleEntry { Name = "Adder", Model = VehicleHash.Adder, Price = 1000000 },
                        new VehicleEntry { Name = "Zentorno", Model = VehicleHash.Zentorno, Price = 1500000 }
                    }
                }
            };
    }
}
