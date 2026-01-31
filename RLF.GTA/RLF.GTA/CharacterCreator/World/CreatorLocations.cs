using GTA;
using GTA.Math;

namespace RLF.GTA.CharacterCreator.World
{
    /// <summary>
    /// Dados de localização para criação
    /// </summary>
    public class CreatorLocation
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public float Heading { get; set; }
        public Weather LocationWeather { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }

        public CreatorLocation()
        {
            Name = "Default";
            Position = Vector3.Zero;
            Heading = 0f;
            LocationWeather = Weather.Clear;
            Hour = 12;
            Minute = 0;
        }

        public CreatorLocation(string name, Vector3 position, float heading, Weather weather, int hour, int minute)
        {
            Name = name;
            Position = position;
            Heading = heading;
            LocationWeather = weather;
            Hour = hour;
            Minute = minute;
        }
    }

    /// <summary>
    /// Locais pré-definidos para criação de personagem
    /// </summary>
    public static class CreatorLocations
    {
        public static readonly CreatorLocation Default = new CreatorLocation(
            "Default",
            new Vector3(402.8f, -996.7f, -100.0f),
            180f,
            Weather.Clear,
            12,
            0
        );

        public static readonly CreatorLocation MichaelHouse = new CreatorLocation(
            "Michael House",
            new Vector3(-813.6f, 179.5f, 72.2f),
            0f,
            Weather.Clear,
            10,
            0
        );

        public static readonly CreatorLocation ApartmentMirror = new CreatorLocation(
            "Apartment Mirror",
            new Vector3(-282.9f, -938.5f, 31.2f),
            180f,
            Weather.Clear,
            14,
            0
        );

        public static readonly CreatorLocation PlasticSurgery = new CreatorLocation(
            "Plastic Surgery",
            new Vector3(-29.5f, -148.3f, 57.1f),
            70f,
            Weather.Clear,
            12,
            0
        );

        public static readonly CreatorLocation Barber = new CreatorLocation(
            "Barber",
            new Vector3(-822.3f, -183.7f, 37.6f),
            0f,
            Weather.Clear,
            15,
            0
        );

        public static CreatorLocation GetLocation(string name)
        {
            switch (name.ToLower())
            {
                case "michael":
                case "michaelhouse":
                    return MichaelHouse;
                case "apartment":
                case "apartmentmirror":
                    return ApartmentMirror;
                case "surgery":
                case "plasticsurgery":
                    return PlasticSurgery;
                case "barber":
                    return Barber;
                default:
                    return Default;
            }
        }
    }
}