using System;
using System.Xml.Serialization;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    [XmlRoot("CharacterData")]
    public class CharacterData
    {
        [XmlElement]
        public string Id { get; set; }

        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public CharacterGender Gender { get; set; }

        [XmlElement]
        public CharacterGenetics Genetics { get; set; }

        [XmlElement]
        public CharacterFaceFeatures FaceFeatures { get; set; }

        [XmlElement]
        public CharacterHair Hair { get; set; }

        [XmlElement]
        public CharacterOverlays Overlays { get; set; }

        [XmlElement]
        public CharacterClothing Clothing { get; set; }

        [XmlElement]
        public CharacterProps Props { get; set; }

        [XmlElement]
        public int EyeColor { get; set; }

        [XmlElement]
        public DateTime CreatedAt { get; set; }

        [XmlElement]
        public DateTime ModifiedAt { get; set; }

        public CharacterData()
        {
            Id = Guid.NewGuid().ToString();
            Name = "Novo Personagem";
            Gender = CharacterGender.Male;
            Genetics = new CharacterGenetics();
            FaceFeatures = new CharacterFaceFeatures();
            Hair = new CharacterHair();
            Overlays = new CharacterOverlays();
            Clothing = new CharacterClothing();
            Props = new CharacterProps();
            EyeColor = 0;
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
        }

        public CharacterData Clone()
        {
            return new CharacterData
            {
                Id = Guid.NewGuid().ToString(),
                Name = this.Name,
                Gender = this.Gender,
                Genetics = this.Genetics.Clone(),
                FaceFeatures = this.FaceFeatures.Clone(),
                Hair = this.Hair.Clone(),
                Overlays = this.Overlays.Clone(),
                Clothing = this.Clothing.Clone(),
                Props = this.Props.Clone(),
                EyeColor = this.EyeColor,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };
        }

        public void Randomize()
        {
            Random rand = new Random();

            // Randomizar genética
            Genetics.ShapeFirst = rand.Next(0, 46);
            Genetics.ShapeSecond = rand.Next(0, 46);
            Genetics.SkinFirst = rand.Next(0, 46);
            Genetics.SkinSecond = rand.Next(0, 46);
            Genetics.ShapeMix = (float)rand.NextDouble();
            Genetics.SkinMix = (float)rand.NextDouble();

            // Randomizar face features (inline)
            for (int i = 0; i < 20; i++)
            {
                float value = (float)(rand.NextDouble() * 2 - 1);
                FaceFeatures.SetFeature((FaceFeature)i, value);
            }

            // Randomizar cabelo
            Hair.Style = rand.Next(0, 50);
            Hair.PrimaryColor = rand.Next(0, 64);
            Hair.HighlightColor = rand.Next(0, 64);

            // Randomizar cor dos olhos
            EyeColor = rand.Next(0, 12);
        }
    }
}