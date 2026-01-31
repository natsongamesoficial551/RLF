using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Gangs
{
    public static class TerritoryDatabase
    {
        private static List<TerritoryData> _territories;
        private static bool _isInitialized = false;

        public static List<TerritoryData> GetAllTerritories()
        {
            if (!_isInitialized) Initialize();
            return new List<TerritoryData>(_territories);
        }

        private static void Initialize()
        {
            _territories = new List<TerritoryData>();

            AddTerritory("families_grove", "Grove Street", 127f, -1930f, 21f, 150f, GangType.Families, 500m, 50);
            AddTerritory("families_davis", "Davis", 45f, -1865f, 22f, 180f, GangType.Families, 400m, 40);
            AddTerritory("families_chamberlain", "Chamberlain Hills", -110f, -1640f, 32f, 200f, GangType.Families, 450m, 45);

            AddTerritory("ballas_rancho", "Rancho", 500f, -1750f, 29f, 150f, GangType.Ballas, 480m, 48);
            AddTerritory("ballas_strawberry", "Strawberry", 270f, -1880f, 26f, 170f, GangType.Ballas, 420m, 42);
            AddTerritory("ballas_davis_south", "Davis South", 90f, -2050f, 18f, 140f, GangType.Ballas, 380m, 38);

            AddTerritory("vagos_cypress", "Cypress Flats", 900f, -2100f, 30f, 200f, GangType.Vagos, 550m, 55);
            AddTerritory("vagos_el_burro", "El Burro Heights", 1350f, -1750f, 65f, 180f, GangType.Vagos, 520m, 52);
            AddTerritory("vagos_rancho_east", "Rancho East", 750f, -1920f, 29f, 150f, GangType.Vagos, 460m, 46);

            AddTerritory("marabunta_la_puerta", "La Puerta", -1050f, -1550f, 5f, 180f, GangType.Marabunta, 600m, 60);
            AddTerritory("marabunta_vespucci", "Vespucci Canals", -1200f, -1420f, 5f, 160f, GangType.Marabunta, 580m, 58);
            AddTerritory("marabunta_textile", "Textile City", 400f, -800f, 29f, 170f, GangType.Marabunta, 540m, 54);

            AddTerritory("armenian_little_seoul", "Little Seoul", -650f, -1220f, 11f, 200f, GangType.ArmenianMob, 750m, 70);
            AddTerritory("armenian_legion", "Legion Square", 220f, -850f, 30f, 150f, GangType.ArmenianMob, 900m, 80);

            AddTerritory("triad_textile", "Textile City Industrial", 500f, -650f, 25f, 180f, GangType.TriadTong, 700m, 65);
            AddTerritory("triad_terminal", "Terminal", 800f, -2900f, 6f, 220f, GangType.TriadTong, 650m, 60);

            AddTerritory("korean_little_seoul_west", "Little Seoul West", -750f, -1050f, 13f, 170f, GangType.KoreanMob, 680m, 63);

            AddTerritory("lost_east_vinewood", "East Vinewood", 980f, -120f, 74f, 200f, GangType.LostMC, 550m, 55);
            AddTerritory("lost_grapeseed", "Grapeseed", 1700f, 4800f, 42f, 250f, GangType.LostMC, 400m, 40);
            AddTerritory("lost_sandy_shores", "Sandy Shores", 1850f, 3700f, 33f, 300f, GangType.LostMC, 450m, 45);

            AddNeutralTerritory("neutral_downtown", "Downtown", -200f, -600f, 34f, 180f, 1000m, 100);
            AddNeutralTerritory("neutral_pillbox", "Pillbox Hill", 50f, -1000f, 29f, 150f, 850m, 85);
            AddNeutralTerritory("neutral_mirror_park", "Mirror Park", 1200f, -600f, 63f, 200f, 720m, 72);
            AddNeutralTerritory("neutral_vinewood_hills", "Vinewood Hills", 100f, 550f, 175f, 250f, 950m, 95);
            AddNeutralTerritory("neutral_paleto", "Paleto Bay", -250f, 6250f, 31f, 300f, 500m, 50);

            _isInitialized = true;
        }

        private static void AddTerritory(string id, string name, float x, float y, float z, float radius,
            GangType controllingGang, decimal dailyIncome, int influence)
        {
            _territories.Add(new TerritoryData
            {
                Id = id,
                Name = name,
                CenterX = x,
                CenterY = y,
                CenterZ = z,
                Radius = radius,
                ControllingGang = controllingGang,
                ControlStrength = 0.8f,
                State = TerritoryControlState.Controlled,
                DailyIncome = dailyIncome,
                InfluencePoints = influence,
                MaxGangMembers = CalculateMaxMembers(radius)
            });
        }

        private static void AddNeutralTerritory(string id, string name, float x, float y, float z,
            float radius, decimal dailyIncome, int influence)
        {
            _territories.Add(new TerritoryData
            {
                Id = id,
                Name = name,
                CenterX = x,
                CenterY = y,
                CenterZ = z,
                Radius = radius,
                ControllingGang = null,
                ControlStrength = 0f,
                State = TerritoryControlState.Neutral,
                DailyIncome = dailyIncome,
                InfluencePoints = influence,
                MaxGangMembers = CalculateMaxMembers(radius)
            });
        }

        private static int CalculateMaxMembers(float radius)
        {
            if (radius >= 300f) return 15;
            if (radius >= 200f) return 10;
            if (radius >= 150f) return 8;
            return 6;
        }

        public static TerritoryData GetTerritoryById(string id)
        {
            if (!_isInitialized) Initialize();
            return _territories.FirstOrDefault(t => t.Id == id);
        }

        public static TerritoryData GetTerritoryAtPosition(float x, float y, float z)
        {
            if (!_isInitialized) Initialize();
            return _territories.FirstOrDefault(t => t.ContainsPosition(x, y, z));
        }

        public static List<TerritoryData> GetTerritoriesByGang(GangType gang)
        {
            if (!_isInitialized) Initialize();
            return _territories.Where(t => t.ControllingGang == gang).ToList();
        }

        public static List<TerritoryData> GetNeutralTerritories()
        {
            if (!_isInitialized) Initialize();
            return _territories.Where(t => !t.ControllingGang.HasValue).ToList();
        }

        public static int GetTerritoryCount(GangType gang)
        {
            if (!_isInitialized) Initialize();
            return _territories.Count(t => t.ControllingGang == gang);
        }

        public static decimal GetTotalIncome(GangType gang)
        {
            if (!_isInitialized) Initialize();
            return _territories.Where(t => t.ControllingGang == gang).Sum(t => t.DailyIncome);
        }

        public static int GetTotalInfluence(GangType gang)
        {
            if (!_isInitialized) Initialize();
            return _territories.Where(t => t.ControllingGang == gang).Sum(t => t.InfluencePoints);
        }
    }
}
