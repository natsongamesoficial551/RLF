using GTA;
using GTA.Math;
using System.Collections.Generic;

namespace RLF.GTA.AmmuNation
{
    /// <summary>
    /// Classe estática contendo todas as coordenadas das Ammu-Nations
    /// </summary>
    public static class AmmuNationLocations
    {
        /// <summary>
        /// Posição fixa do interior (usada para todas as lojas)
        /// </summary>
        public static readonly Vector3 InteriorPosition = new Vector3(22.09f, -1107.28f, 29.80f);

        /// <summary>
        /// Heading do player ao entrar no interior
        /// </summary>
        public static readonly float InteriorHeading = 0.0f;

        /// <summary>
        /// Lista de todas as entradas externas das Ammu-Nations
        /// </summary>
        public static readonly List<AmmuNationLocation> ExternalLocations = new List<AmmuNationLocation>
        {
            new AmmuNationLocation("Hawick", new Vector3(242.650f, -44.801f, 69.897f)), // correto
            new AmmuNationLocation("Adam's Apple Blvd", new Vector3(844.055f, -1023.223f, 27.910f)), // correto
            new AmmuNationLocation("Paleto Bay", new Vector3(-324.484f, 6045.487f, 31.242f)), // correto
            new AmmuNationLocation("Little Seoul", new Vector3(-664.0f, -945.396f, 21.635f)), // correto
            new AmmuNationLocation("Morningwood", new Vector3(-1315.413f, -390.104f, 36.540f)), // correto
            new AmmuNationLocation("Chumash", new Vector3(-1111.943f, 2689.669f, 18.594f)), // correto
            new AmmuNationLocation("Pillbox Hill", new Vector3(17.131f, -1116.180f, 29.791f)), // correto
            new AmmuNationLocation("Cypress Flats", new Vector3(3163.194f, -1082.151f, 20.848f)), // correto
            new AmmuNationLocation("Sandy Shores", new Vector3(2569.469f, 304.430f, 108.608f)), // correto
            new AmmuNationLocation("Palomino Freeway", new Vector3(1699.983f, 3751.774f, 34.366f)), // correto
            new AmmuNationLocation("Grapeseed", new Vector3(-811.845f, -2147.534f, 29.503f)) // correto
        };
    }

    /// <summary>
    /// Representa uma localização externa de Ammu-Nation
    /// </summary>
    public class AmmuNationLocation
    {
        public string Name { get; }
        public Vector3 Position { get; }

        public AmmuNationLocation(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }
    }
}