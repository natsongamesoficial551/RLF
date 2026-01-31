using System.Collections.Generic;
using GTA.Math;
using GTA;

namespace RLF.GTA.Jobs.Postal
{
    public static class PostalConfig
    {
        // Bicicleta usada para entregas
        public static readonly VehicleHash PostalBike = VehicleHash.Cruiser;

        // ✅ Locais de spawn de bicicletas (lojas 24/7 e correios)
        public static readonly List<Vector3> BikePickupLocations = new List<Vector3>
        {
            // ===== LOS SANTOS (24/7 e Correios) =====
            new Vector3(-47.5f, -1757.5f, 29.4f),        // Grove Street 24/7
            new Vector3(-707.3f, -913.6f, 19.2f),        // Little Seoul 24/7
            new Vector3(24.5f, -1346.2f, 29.5f),         // Davis 24/7
            new Vector3(372.8f, 326.4f, 103.5f),         // Clinton Ave 24/7
            new Vector3(1163.4f, -323.8f, 69.2f),        // Mirror Park 24/7
            new Vector3(-1222.7f, -906.9f, 12.3f),       // Vespucci 24/7
            new Vector3(-1486.0f, -379.0f, 40.2f),       // Morningwood 24/7
            new Vector3(1134.2f, -982.7f, 46.4f),        // El Rancho Blvd 24/7
            new Vector3(1165.3f, 2710.8f, 38.1f),        // Harmony 24/7
            new Vector3(-3040.0f, 585.0f, 7.9f),         // Chumash 24/7
            
            // ===== SANDY SHORES =====
            new Vector3(1961.3f, 3740.5f, 32.3f),        // Sandy Shores Main Street
            new Vector3(1960.5f, 3750.1f, 32.2f),        // Sandy Shores 24/7
            new Vector3(1701.2f, 3753.8f, 34.0f),        // Sandy Shores Motel
            
            // ===== PALETO BAY =====
            new Vector3(-275.0f, 6226.0f, 31.5f),        // Paleto Bay 24/7
            new Vector3(168.0f, 6634.0f, 31.7f),         // Paleto Bay Sheriff
            
            // ===== HARMONY =====
            new Vector3(1207.0f, 2660.0f, 37.9f),        // Harmony Gas Station
            new Vector3(615.0f, 2762.0f, 42.0f)          // Harmony Village
        };

        // ✅ Endereços de entrega de correspondências (residências)
        public static readonly List<Vector3> DeliveryAddresses = new List<Vector3>
        {
            // ===== LOS SANTOS SUL (Áreas Residenciais) =====
            new Vector3(-1158.5f, -992.4f, 2.2f),        // Vespucci Beach
            new Vector3(-1215.3f, -1016.8f, 2.1f),       // Vespucci Beach
            new Vector3(-1285.7f, -1147.2f, 6.8f),       // Vespucci Canals
            new Vector3(-1344.2f, -1146.5f, 4.5f),       // Vespucci Canals
            new Vector3(-1405.8f, -977.3f, 9.3f),        // Del Perro Beach
            new Vector3(-1518.6f, -909.7f, 10.0f),       // Del Perro Beach
            new Vector3(-1623.4f, -1037.2f, 13.2f),      // Del Perro
            new Vector3(-1729.8f, -1090.5f, 13.2f),      // Del Perro
            new Vector3(-1884.3f, -585.4f, 11.9f),       // Pacific Bluffs
            new Vector3(-1954.7f, -524.3f, 12.2f),       // Pacific Bluffs
            new Vector3(-2007.4f, -488.2f, 11.6f),       // Pacific Bluffs
            
            // ===== LOS SANTOS CENTRO =====
            new Vector3(-1096.5f, -1673.2f, 4.4f),       // Del Perro Pier
            new Vector3(-1049.8f, -1527.3f, 5.0f),       // Vespucci
            new Vector3(-967.3f, -1432.8f, 5.2f),        // Vespucci
            new Vector3(-883.2f, -1366.5f, 5.2f),        // Little Seoul
            new Vector3(-802.5f, -1312.4f, 5.2f),        // Little Seoul
            new Vector3(-721.3f, -1268.9f, 5.2f),        // Little Seoul
            new Vector3(-594.7f, -1146.8f, 22.2f),       //Koreantown
            new Vector3(-501.8f, -1096.5f, 23.3f),       // Koreantown
            new Vector3(-399.2f, -982.7f, 29.4f),        // Burton
            
            // ===== LOS SANTOS LESTE =====
            new Vector3(115.3f, -1729.5f, 29.3f),        // Strawberry
            new Vector3(232.4f, -1750.8f, 29.0f),        // Strawberry
            new Vector3(348.7f, -1821.3f, 28.9f),        // Davis
            new Vector3(442.3f, -1895.4f, 26.7f),        // Davis
            new Vector3(534.8f, -1974.2f, 24.8f),        // Banning
            new Vector3(-70.3f, -1526.8f, 33.8f),        // Chamberlain Hills
            new Vector3(-156.7f, -1612.3f, 33.7f),       // Chamberlain Hills
            new Vector3(-253.4f, -1673.9f, 33.9f),       // Chamberlain Hills
            new Vector3(1250.8f, -620.5f, 69.6f),        // Mirror Park
            new Vector3(1357.2f, -596.4f, 74.3f),        // Mirror Park
            
            // ===== VINEWOOD / HILLS =====
            new Vector3(-1821.37f, 794.97f, 138.09f),    // Richman
            new Vector3(-1923.0f, 595.0f, 122.5f),       // Richman
            new Vector3(-2009.0f, 367.0f, 94.8f),        // Richman Hills
            new Vector3(318.70f, -229.39f, 54.22f),      // Vinewood
            new Vector3(-165.5f, 232.7f, 94.9f),         // Vinewood Hills
            
            // ===== SANDY SHORES =====
            new Vector3(1900.0f, 3714.0f, 32.8f),        // Sandy Shores Residencial
            new Vector3(2000.0f, 3789.0f, 32.2f),        // Sandy Shores
            new Vector3(1880.0f, 3810.0f, 33.0f),        // Sandy Shores
            new Vector3(1662.0f, 3820.0f, 34.8f),        // Sandy Shores Motel
            new Vector3(1437.0f, 3656.0f, 34.9f),        // Sandy Shores Airfield
            new Vector3(1319.0f, 3618.0f, 33.7f),        // Sandy Shores
            new Vector3(1725.0f, 4642.0f, 43.0f),        // Grapeseed
            new Vector3(1656.0f, 4746.0f, 42.0f),        // Grapeseed Farm
            
            // ===== PALETO BAY =====
            new Vector3(-378.0f, 6253.0f, 31.5f),        // Paleto Bay
            new Vector3(-214.0f, 6406.0f, 31.3f),        // Paleto Bay
            new Vector3(92.0f, 6610.0f, 31.6f),          // Paleto Bay East
            new Vector3(1670.0f, 6426.0f, 32.4f),        // Paleto Bay East
            new Vector3(1777.0f, 6408.0f, 34.3f),        // Paleto Bay East
            
            // ===== HARMONY / GRAND SENORA =====
            new Vector3(1143.0f, 2664.0f, 38.0f),        // Harmony
            new Vector3(540.0f, 2671.0f, 42.3f),         // Harmony
            new Vector3(378.0f, 2638.0f, 44.5f),         // Grand Senora
            new Vector3(191.0f, 3029.0f, 43.9f),         // Grand Senora Desert
            
            // ===== NORTE DE LOS SANTOS =====
            new Vector3(-3086.0f, 339.0f, 6.4f),         // Chumash
            new Vector3(-2952.0f, 470.0f, 15.5f),        // Chumash
            new Vector3(-1408.0f, -518.0f, 31.5f),       // Morningwood
            new Vector3(-1327.0f, -441.0f, 35.6f),       // Morningwood
            new Vector3(-1562.0f, -407.0f, 42.4f)        // Del Perro Beach
        };
    }
}
