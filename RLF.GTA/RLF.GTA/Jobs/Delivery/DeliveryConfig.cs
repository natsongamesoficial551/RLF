using System.Collections.Generic;
using GTA.Math;
using GTA;

namespace RLF.GTA.Jobs.Delivery
{
    public static class DeliveryConfig
    {
        public static readonly VehicleHash DeliveryVehicle = VehicleHash.Faggio;

        // ✅ CORRIGIDO: Agora inclui locais em TODO o mapa
        public static readonly List<Vector3> PickupLocations = new List<Vector3>
        {
            // ===== LOS SANTOS (Centro/Sul) =====
            new Vector3(-1182.9f, -884.3f, 13.8f),      // Vespucci
            new Vector3(-1201.3f, -892.5f, 13.8f),      // Vespucci
            new Vector3(-1195.7f, -898.2f, 13.8f),      // Vespucci
            new Vector3(128.1f, -1286.7f, 29.3f),       // Strawberry
            new Vector3(73.9f, -1392.5f, 29.4f),        // Davis
            new Vector3(24.5f, -1346.2f, 29.5f),        // Davis
            new Vector3(-707.3f, -913.6f, 19.2f),       // Little Seoul
            new Vector3(-1305.4f, -834.3f, 17.1f),      // Del Perro
            new Vector3(1163.4f, -323.8f, 69.2f),       // Mirror Park
            new Vector3(-165.5f, 232.7f, 94.9f),        // Vinewood Hills
            
            // ===== SANDY SHORES =====
            new Vector3(1961.3f, 3740.5f, 32.3f),       // Sandy Shores Main Street
            new Vector3(1960.5f, 3750.1f, 32.2f),       // Sandy Shores 24/7
            new Vector3(1701.2f, 3753.8f, 34.0f),       // Sandy Shores Motel
            new Vector3(1392.6f, 3606.4f, 34.9f),       // Sandy Shores Airfield
            new Vector3(1687.0f, 4820.0f, 42.0f),       // Grapeseed
            new Vector3(1698.5f, 4924.0f, 42.0f),       // Grapeseed Farm
            
            // ===== PALETO BAY =====
            new Vector3(-275.0f, 6226.0f, 31.5f),       // Paleto Bay 24/7
            new Vector3(-161.0f, 6321.0f, 31.5f),       // Paleto Bay Center
            new Vector3(1729.5f, 6414.5f, 35.0f),       // Paleto Bay East
            new Vector3(168.0f, 6634.0f, 31.7f),        // Paleto Bay Sheriff
            
            // ===== HARMONY / GRAND SENORA =====
            new Vector3(1207.0f, 2660.0f, 37.9f),       // Harmony Gas Station
            new Vector3(615.0f, 2762.0f, 42.0f),        // Harmony Village
            new Vector3(265.0f, 2597.0f, 44.8f),        // Grand Senora Desert
            
            // ===== NORTE DE LOS SANTOS =====
            new Vector3(-1820.0f, 792.5f, 138.1f),      // Richman
            new Vector3(-3040.0f, 585.0f, 7.9f),        // Chumash
            new Vector3(-2966.0f, 391.0f, 15.0f),       // Great Ocean Highway
            new Vector3(-1486.0f, -379.0f, 40.2f),      // Morningwood
            
            // ===== VINEWOOD / DOWNTOWN =====
            new Vector3(373.0f, 327.0f, 103.5f),        // Vinewood
            new Vector3(-1562.0f, -407.0f, 42.4f),      // Del Perro Beach
            new Vector3(-526.0f, -1223.0f, 18.4f)       // Vespucci Canals
        };

        // ✅ Endereços de entrega expandidos para todo o mapa
        public static readonly List<Vector3> DeliveryAddresses = new List<Vector3>
        {
            // ===== LOS SANTOS SUL =====
            new Vector3(-1158.5f, -992.4f, 2.2f),
            new Vector3(-1215.3f, -1016.8f, 2.1f),
            new Vector3(-1285.7f, -1147.2f, 6.8f),
            new Vector3(-1344.2f, -1146.5f, 4.5f),
            new Vector3(-1405.8f, -977.3f, 9.3f),
            new Vector3(-1518.6f, -909.7f, 10.0f),
            new Vector3(-1623.4f, -1037.2f, 13.2f),
            new Vector3(-1729.8f, -1090.5f, 13.2f),
            new Vector3(-1884.3f, -585.4f, 11.9f),
            new Vector3(-1954.7f, -524.3f, 12.2f),
            new Vector3(-2007.4f, -488.2f, 11.6f),
            
            // ===== LOS SANTOS CENTRO =====
            new Vector3(-1096.5f, -1673.2f, 4.4f),
            new Vector3(-1049.8f, -1527.3f, 5.0f),
            new Vector3(-967.3f, -1432.8f, 5.2f),
            new Vector3(-883.2f, -1366.5f, 5.2f),
            new Vector3(-802.5f, -1312.4f, 5.2f),
            new Vector3(-721.3f, -1268.9f, 5.2f),
            new Vector3(-594.7f, -1146.8f, 22.2f),
            new Vector3(-501.8f, -1096.5f, 23.3f),
            new Vector3(-399.2f, -982.7f, 29.4f),
            
            // ===== LOS SANTOS LESTE =====
            new Vector3(115.3f, -1729.5f, 29.3f),
            new Vector3(232.4f, -1750.8f, 29.0f),
            new Vector3(348.7f, -1821.3f, 28.9f),
            new Vector3(442.3f, -1895.4f, 26.7f),
            new Vector3(534.8f, -1974.2f, 24.8f),
            new Vector3(-70.3f, -1526.8f, 33.8f),
            new Vector3(-156.7f, -1612.3f, 33.7f),
            new Vector3(-253.4f, -1673.9f, 33.9f),
            new Vector3(1250.8f, -620.5f, 69.6f),
            new Vector3(1357.2f, -596.4f, 74.3f),
            
            // ===== SANDY SHORES =====
            new Vector3(1900.0f, 3714.0f, 32.8f),       // Casa residencial
            new Vector3(2000.0f, 3789.0f, 32.2f),       // Residência Sandy
            new Vector3(1880.0f, 3810.0f, 33.0f),       // Casa Sandy
            new Vector3(1662.0f, 3820.0f, 34.8f),       // Residência Motel
            new Vector3(1437.0f, 3656.0f, 34.9f),       // Casa perto Airfield
            new Vector3(1319.0f, 3618.0f, 33.7f),       // Residência Airfield
            new Vector3(1725.0f, 4642.0f, 43.0f),       // Casa Grapeseed
            new Vector3(1656.0f, 4746.0f, 42.0f),       // Residência Grapeseed
            
            // ===== PALETO BAY =====
            new Vector3(-378.0f, 6253.0f, 31.5f),       // Casa Paleto
            new Vector3(-214.0f, 6406.0f, 31.3f),       // Residência Paleto
            new Vector3(92.0f, 6610.0f, 31.6f),         // Casa Paleto East
            new Vector3(1670.0f, 6426.0f, 32.4f),       // Residência Paleto East
            new Vector3(1777.0f, 6408.0f, 34.3f),       // Casa East Paleto
            
            // ===== HARMONY / GRAND SENORA =====
            new Vector3(1143.0f, 2664.0f, 38.0f),       // Casa Harmony
            new Vector3(540.0f, 2671.0f, 42.3f),        // Residência Harmony
            new Vector3(378.0f, 2638.0f, 44.5f),        // Casa Grand Senora
            new Vector3(191.0f, 3029.0f, 43.9f),        // Residência Desert
            
            // ===== NORTE DE LOS SANTOS =====
            new Vector3(-1923.0f, 595.0f, 122.5f),      // Casa Richman
            new Vector3(-2009.0f, 367.0f, 94.8f),       // Residência Richman
            new Vector3(-3086.0f, 339.0f, 6.4f),        // Casa Chumash
            new Vector3(-2952.0f, 470.0f, 15.5f),       // Residência Chumash
            new Vector3(-1408.0f, -518.0f, 31.5f),      // Casa Morningwood
            new Vector3(-1327.0f, -441.0f, 35.6f)       // Residência Morningwood
        };
    }
}