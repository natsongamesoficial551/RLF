using GTA;
using GTA.Math;
using System.Collections.Generic;

namespace RLF.GTA.Jobs.Postal
{
    /// <summary>
    /// Classe estática contendo todas as coordenadas dos pontos de trabalho de Carteiro
    /// </summary>
    public static class PostalWorkLocations
    {
        /// <summary>
        /// Lista de todos os pontos de trabalho (lojas 24/7)
        /// </summary>
        public static readonly List<PostalWorkLocation> WorkLocations = new List<PostalWorkLocation>
        {
            // ✅ Los Santos (Centro)
            new PostalWorkLocation(
                "Correios - Little Seoul", 
                new Vector3(-707.3f, -913.6f, 19.2f),  // 24/7 Little Seoul
                new Vector3(-706.5f, -915.0f, 19.2f)   // Posição de spawn da bike
            ),

            // ✅ Los Santos (Norte/Richman)
            new PostalWorkLocation(
                "Correios - Morningwood", 
                new Vector3(-1486.0f, -379.0f, 40.2f),  // 24/7 Morningwood
                new Vector3(-1487.5f, -380.5f, 40.2f)   // Posição de spawn da bike
            ),

            // ✅ Sandy Shores (Interior)
            new PostalWorkLocation(
                "Correios - Sandy Shores", 
                new Vector3(1961.3f, 3740.5f, 32.3f),  // 24/7 Sandy Shores
                new Vector3(1959.0f, 3742.0f, 32.3f)   // Posição de spawn da bike
            )
        };
    }

    /// <summary>
    /// Representa um ponto de trabalho de Carteiro
    /// </summary>
    public class PostalWorkLocation
    {
        public string Name { get; }
        public Vector3 InteractionPosition { get; }
        public Vector3 BikeSpawnPosition { get; }

        public PostalWorkLocation(string name, Vector3 interactionPosition, Vector3 bikeSpawnPosition)
        {
            Name = name;
            InteractionPosition = interactionPosition;
            BikeSpawnPosition = bikeSpawnPosition;
        }
    }
}
