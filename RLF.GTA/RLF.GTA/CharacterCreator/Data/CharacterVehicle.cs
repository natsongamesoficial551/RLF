using System;
using System.Xml.Serialization;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class CharacterVehicle
    {
        [XmlElement]
        public string Model { get; set; }

        [XmlElement]
        public int PrimaryColor { get; set; }

        [XmlElement]
        public int SecondaryColor { get; set; }

        [XmlElement]
        public int PearlescentColor { get; set; }

        [XmlElement]
        public int WheelColor { get; set; }

        [XmlElement]
        public float PositionX { get; set; }

        [XmlElement]
        public float PositionY { get; set; }

        [XmlElement]
        public float PositionZ { get; set; }

        [XmlElement]
        public float Heading { get; set; }

        [XmlElement]
        public string LicensePlate { get; set; }

        [XmlElement]
        public int LicensePlateStyle { get; set; }

        [XmlElement]
        public bool HasVehicle { get; set; }

        [XmlElement]
        public bool WasInVehicle { get; set; }

        public CharacterVehicle()
        {
            Model = "";
            PrimaryColor = 0;
            SecondaryColor = 0;
            PearlescentColor = 0;
            WheelColor = 0;
            PositionX = 0f;
            PositionY = 0f;
            PositionZ = 0f;
            Heading = 0f;
            LicensePlate = "";
            LicensePlateStyle = 0;
            HasVehicle = false;
            WasInVehicle = false;
        }

        public void Clear()
        {
            Model = "";
            PrimaryColor = 0;
            SecondaryColor = 0;
            PearlescentColor = 0;
            WheelColor = 0;
            PositionX = 0f;
            PositionY = 0f;
            PositionZ = 0f;
            Heading = 0f;
            LicensePlate = "";
            LicensePlateStyle = 0;
            HasVehicle = false;
            WasInVehicle = false;
        }

        public CharacterVehicle Clone()
        {
            return new CharacterVehicle
            {
                Model = this.Model,
                PrimaryColor = this.PrimaryColor,
                SecondaryColor = this.SecondaryColor,
                PearlescentColor = this.PearlescentColor,
                WheelColor = this.WheelColor,
                PositionX = this.PositionX,
                PositionY = this.PositionY,
                PositionZ = this.PositionZ,
                Heading = this.Heading,
                LicensePlate = this.LicensePlate,
                LicensePlateStyle = this.LicensePlateStyle,
                HasVehicle = this.HasVehicle,
                WasInVehicle = this.WasInVehicle
            };
        }
    }
}