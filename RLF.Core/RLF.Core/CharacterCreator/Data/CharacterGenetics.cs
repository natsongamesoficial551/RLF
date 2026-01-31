using System;
using System.Xml.Serialization;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class CharacterGenetics
    {
        [XmlElement]
        public int ShapeFirst { get; set; }

        [XmlElement]
        public int ShapeSecond { get; set; }

        [XmlElement]
        public int ShapeThird { get; set; }

        [XmlElement]
        public int SkinFirst { get; set; }

        [XmlElement]
        public int SkinSecond { get; set; }

        [XmlElement]
        public int SkinThird { get; set; }

        [XmlElement]
        public float ShapeMix { get; set; }

        [XmlElement]
        public float SkinMix { get; set; }

        [XmlElement]
        public float ThirdMix { get; set; }

        public CharacterGenetics()
        {
            ShapeFirst = 0;
            ShapeSecond = 0;
            ShapeThird = 0;
            SkinFirst = 0;
            SkinSecond = 0;
            SkinThird = 0;
            ShapeMix = 0.5f;
            SkinMix = 0.5f;
            ThirdMix = 0f;
        }

        public CharacterGenetics Clone()
        {
            return new CharacterGenetics
            {
                ShapeFirst = this.ShapeFirst,
                ShapeSecond = this.ShapeSecond,
                ShapeThird = this.ShapeThird,
                SkinFirst = this.SkinFirst,
                SkinSecond = this.SkinSecond,
                SkinThird = this.SkinThird,
                ShapeMix = this.ShapeMix,
                SkinMix = this.SkinMix,
                ThirdMix = this.ThirdMix
            };
        }
    }
}