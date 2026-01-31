using RLF.Core.CharacterCreator.Enums;
using System;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class CharacterHair
    {
        public int Style { get; set; }
        public int PrimaryColor { get; set; }
        public int HighlightColor { get; set; }

        public CharacterHair()
        {
            Style = 0;
            PrimaryColor = 0;
            HighlightColor = 0;
        }

        public CharacterHair Clone()
        {
            return new CharacterHair
            {
                Style = this.Style,
                PrimaryColor = this.PrimaryColor,
                HighlightColor = this.HighlightColor
            };
        }

        public static CharacterHair CreateRandom(Random random)
        {
            return new CharacterHair
            {
                Style = random.Next(0, 36),
                PrimaryColor = random.Next(0, 64),
                HighlightColor = random.Next(0, 64)
            };
        }
    }

    [Serializable]
    public class CharacterFaceFeatures
    {
        private float[] _features;

        public CharacterFaceFeatures()
        {
            _features = new float[20];
        }

        public float GetFeature(FaceFeature feature)
        {
            int index = (int)feature;
            if (index >= 0 && index < _features.Length)
                return _features[index];
            return 0f;
        }

        public void SetFeature(FaceFeature feature, float value)
        {
            int index = (int)feature;
            if (index >= 0 && index < _features.Length)
            {
                if (value < -1f) value = -1f;
                if (value > 1f) value = 1f;
                _features[index] = value;
            }
        }

        public CharacterFaceFeatures Clone()
        {
            var clone = new CharacterFaceFeatures();
            for (int i = 0; i < _features.Length; i++)
            {
                clone._features[i] = _features[i];
            }
            return clone;
        }

        public static CharacterFaceFeatures CreateRandom(Random random)
        {
            var features = new CharacterFaceFeatures();
            for (int i = 0; i < 20; i++)
            {
                features._features[i] = ((float)random.NextDouble() * 2f) - 1f;
            }
            return features;
        }
    }
}