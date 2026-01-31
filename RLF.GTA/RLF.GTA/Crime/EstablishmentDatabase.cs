using System.Collections.Generic;
using GTA.Math;

namespace RLF.Core.Crime.Establishments
{
    /// <summary>
    /// Database com todos os estabelecimentos roubáveis do GTA V.
    /// Posições reais do mapa.
    /// </summary>
    public static class EstablishmentDatabase
    {
        private static List<EstablishmentData> _establishments;
        private static bool _isInitialized = false;

        public static List<EstablishmentData> GetAllEstablishments()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return new List<EstablishmentData>(_establishments);
        }

        private static void Initialize()
        {
            _establishments = new List<EstablishmentData>();

            // ===== LOJAS 24/7 (24-Seven Stores) =====
            AddStore24_7("247_1", "24/7 Supermarket - Innocence Blvd", new Vector3(24.47f, -1346.19f, 29.5f));
            AddStore24_7("247_2", "24/7 Supermarket - Clinton Ave", new Vector3(-3039.54f, 584.38f, 7.91f));
            AddStore24_7("247_3", "24/7 Supermarket - Alhambra Dr", new Vector3(-3242.23f, 1000.05f, 12.83f));
            AddStore24_7("247_4", "24/7 Supermarket - Barbareno Rd", new Vector3(1728.78f, 6415.76f, 35.04f));
            AddStore24_7("247_5", "24/7 Supermarket - Route 68", new Vector3(1697.87f, 4923.42f, 42.06f));
            AddStore24_7("247_6", "24/7 Supermarket - Banham Canyon Dr", new Vector3(-1820.82f, 792.52f, 138.12f));
            AddStore24_7("247_7", "24/7 Supermarket - Senora Fwy", new Vector3(1959.19f, 3740.48f, 32.34f));
            AddStore24_7("247_8", "24/7 Supermarket - Harmony", new Vector3(548.38f, 2670.36f, 42.16f));
            AddStore24_7("247_9", "24/7 Supermarket - Grand Senora Desert", new Vector3(2677.47f, 3279.76f, 55.24f));
            AddStore24_7("247_10", "24/7 Supermarket - Palomino Fwy", new Vector3(2556.75f, 380.84f, 108.62f));
            AddStore24_7("247_11", "24/7 Supermarket - Mirror Park", new Vector3(372.72f, 326.89f, 103.57f));

            // ===== LIQUOR STORES (Rob's Liquor) =====
            AddLiquorStore("liquor_1", "Rob's Liquor - El Rancho Blvd", new Vector3(1134.15f, -982.38f, 46.42f));
            AddLiquorStore("liquor_2", "Rob's Liquor - Prosperity St", new Vector3(-1486.29f, -377.68f, 40.16f));
            AddLiquorStore("liquor_3", "Rob's Liquor - San Andreas Ave", new Vector3(-1221.42f, -908.54f, 12.33f));
            AddLiquorStore("liquor_4", "Rob's Liquor - Great Ocean Hwy", new Vector3(-2966.39f, 390.92f, 15.04f));
            AddLiquorStore("liquor_5", "Rob's Liquor - Senora Fwy", new Vector3(1165.05f, 2710.85f, 38.16f));

            // ===== AMMU-NATION (Lojas de Armas) =====
            AddAmmuNation("ammu_1", "Ammu-Nation - Pillbox Hill", new Vector3(22.53f, -1105.93f, 29.8f));
            AddAmmuNation("ammu_2", "Ammu-Nation - Little Seoul", new Vector3(842.54f, -1035.25f, 28.19f));
            AddAmmuNation("ammu_3", "Ammu-Nation - Cypress Flats", new Vector3(810.22f, -2158.99f, 29.62f));
            AddAmmuNation("ammu_4", "Ammu-Nation - La Mesa", new Vector3(1692.41f, 3760.91f, 34.71f));
            AddAmmuNation("ammu_5", "Ammu-Nation - Sandy Shores", new Vector3(1693.95f, 3761.60f, 34.71f));
            AddAmmuNation("ammu_6", "Ammu-Nation - Paleto Bay", new Vector3(-330.35f, 6084.86f, 31.45f));
            AddAmmuNation("ammu_7", "Ammu-Nation - Morningwood", new Vector3(-662.27f, -933.58f, 21.83f));
            AddAmmuNation("ammu_8", "Ammu-Nation - Chumash", new Vector3(-3173.51f, 1088.35f, 20.84f));
            AddAmmuNation("ammu_9", "Ammu-Nation - Harmony", new Vector3(252.89f, -50.00f, 69.94f));
            AddAmmuNation("ammu_10", "Ammu-Nation - Grand Senora Desert", new Vector3(2567.98f, 292.62f, 108.73f));

            // ===== FLEECA BANKS (Bancos Pequenos) =====
            AddFleecaBank("fleeca_1", "Fleeca Bank - Legion Square", new Vector3(146.92f, -1046.11f, 29.37f));
            AddFleecaBank("fleeca_2", "Fleeca Bank - Burton", new Vector3(-351.26f, -51.28f, 49.04f));
            AddFleecaBank("fleeca_3", "Fleeca Bank - Hawick Ave", new Vector3(313.43f, -280.45f, 54.16f));
            AddFleecaBank("fleeca_4", "Fleeca Bank - Del Perro", new Vector3(-1211.87f, -336.17f, 37.78f));
            AddFleecaBank("fleeca_5", "Fleeca Bank - Great Ocean Hwy", new Vector3(-2957.63f, 481.81f, 15.70f));
            AddFleecaBank("fleeca_6", "Fleeca Bank - Route 68", new Vector3(1175.05f, 2712.90f, 38.09f));

            // ===== PACIFIC STANDARD BANK (Banco Grande) =====
            AddPacificBank("pacific_1", "Pacific Standard Bank - Vinewood", new Vector3(255.00f, 225.00f, 101.88f));

            _isInitialized = true;
        }

        private static void AddStore24_7(string id, string name, Vector3 position)
        {
            _establishments.Add(new EstablishmentData
            {
                Id = id,
                Type = EstablishmentType.Store24_7,
                Name = name,
                Position = position,
                CounterPosition = position + new Vector3(1f, 1f, 0f), // Aproximado
                HasAlarm = true,
                HasSafe = false,
                HasCameras = true,
                AlarmTriggerChance = 0.4f,
                MinCashRegister = 200m,
                MaxCashRegister = 800m,
                CooldownHours = 0.5f, // 30 minutos
                ClerkPedModel = "s_m_m_linecook"
            });
        }

        private static void AddLiquorStore(string id, string name, Vector3 position)
        {
            _establishments.Add(new EstablishmentData
            {
                Id = id,
                Type = EstablishmentType.LiquorStore,
                Name = name,
                Position = position,
                CounterPosition = position + new Vector3(1f, 1f, 0f),
                HasAlarm = true,
                HasSafe = true,
                HasCameras = true,
                AlarmTriggerChance = 0.35f,
                MinCashRegister = 300m,
                MaxCashRegister = 1000m,
                MinSafeMoney = 500m,
                MaxSafeMoney = 2000m,
                SafeOpenTime = 10f, // 45 segundos
                CooldownHours = 1.0f,
                ClerkPedModel = "s_m_m_linecook"
            });
        }

        private static void AddAmmuNation(string id, string name, Vector3 position)
        {
            _establishments.Add(new EstablishmentData
            {
                Id = id,
                Type = EstablishmentType.AmmuNation,
                Name = name,
                Position = position,
                CounterPosition = position + new Vector3(2f, 0f, 0f),
                HasAlarm = true,
                HasSafe = true,
                HasCameras = true,
                AlarmTriggerChance = 0.5f, // Maior chance (loja de armas)
                MinCashRegister = 500m,
                MaxCashRegister = 1500m,
                MinSafeMoney = 1000m,
                MaxSafeMoney = 4000m,
                SafeOpenTime = 10f, // 1 minuto
                CooldownHours = 2.0f,
                ClerkPedModel = "s_m_y_ammucity_01"
            });
        }

        private static void AddFleecaBank(string id, string name, Vector3 position)
        {
            _establishments.Add(new EstablishmentData
            {
                Id = id,
                Type = EstablishmentType.FleecaBank,
                Name = name,
                Position = position,
                CounterPosition = position + new Vector3(3f, 2f, 0f),
                SafePosition = position + new Vector3(-5f, 5f, 0f),
                HasAlarm = true,
                HasSafe = true,
                HasCameras = true,
                AlarmTriggerChance = 0.7f, // ALTA chance
                MinCashRegister = 2000m,
                MaxCashRegister = 5000m,
                MinSafeMoney = 10000m,
                MaxSafeMoney = 30000m,
                SafeOpenTime = 10f, // 2 minutos
                CooldownHours = 6.0f, // 6 horas
                ClerkPedModel = "s_m_m_banker_01"
            });
        }

        private static void AddPacificBank(string id, string name, Vector3 position)
        {
            _establishments.Add(new EstablishmentData
            {
                Id = id,
                Type = EstablishmentType.PacificBank,
                Name = name,
                Position = position,
                CounterPosition = position + new Vector3(5f, 5f, 0f),
                SafePosition = position + new Vector3(-10f, 10f, -5f), // Vault no subsolo
                HasAlarm = true,
                HasSafe = true,
                HasCameras = true,
                AlarmTriggerChance = 0.9f, // MUITO ALTA
                MinCashRegister = 0m, // Sem caixa - só vault
                MaxCashRegister = 0m,
                MinSafeMoney = 50000m,
                MaxSafeMoney = 150000m,
                SafeOpenTime = 10f, // 5 minutos!
                CooldownHours = 24.0f, // 24 horas (1 dia)
                ClerkPedModel = "s_m_m_banker_01"
            });
        }

        public static EstablishmentData GetEstablishmentById(string id)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _establishments.Find(e => e.Id == id);
        }

        public static EstablishmentData GetNearestEstablishment(Vector3 position, float maxDistance = 50f)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            EstablishmentData nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var establishment in _establishments)
            {
                float distance = establishment.Position.DistanceTo(position);
                if (distance < nearestDistance && distance <= maxDistance)
                {
                    nearestDistance = distance;
                    nearest = establishment;
                }
            }

            return nearest;
        }
    }
}