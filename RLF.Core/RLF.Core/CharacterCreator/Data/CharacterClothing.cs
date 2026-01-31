using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class ClothingItem
    {
        [XmlElement]
        public ComponentType Type { get; set; }

        [XmlElement]
        public int DrawableId { get; set; }

        [XmlElement]
        public int TextureId { get; set; }

        public ClothingItem()
        {
            Type = ComponentType.Torso;
            DrawableId = 0;
            TextureId = 0;
        }

        public ClothingItem(ComponentType type, int drawable, int texture)
        {
            Type = type;
            DrawableId = drawable;
            TextureId = texture;
        }

        public ClothingItem Clone()
        {
            return new ClothingItem(Type, DrawableId, TextureId);
        }
    }

    [Serializable]
    public class CharacterClothing
    {
        [XmlArray("Components")]
        [XmlArrayItem("Component")]
        public List<ClothingItem> Components { get; set; }

        public CharacterClothing()
        {
            Components = new List<ClothingItem>();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
            {
                Components.Add(new ClothingItem(type, 0, 0));
            }
        }

        public void SetComponent(ComponentType type, int drawable, int texture)
        {
            var existing = Components.Find(c => c.Type == type);
            if (existing != null)
            {
                existing.DrawableId = drawable;
                existing.TextureId = texture;
            }
            else
            {
                Components.Add(new ClothingItem(type, drawable, texture));
            }
        }

        public ClothingItem GetComponent(ComponentType type)
        {
            return Components.Find(c => c.Type == type);
        }

        public int GetDrawable(ComponentType type)
        {
            var item = GetComponent(type);
            return item?.DrawableId ?? 0;
        }

        public int GetTexture(ComponentType type)
        {
            var item = GetComponent(type);
            return item?.TextureId ?? 0;
        }

        public CharacterClothing Clone()
        {
            var clone = new CharacterClothing();
            clone.Components.Clear();

            foreach (var item in Components)
            {
                clone.Components.Add(item.Clone());
            }

            return clone;
        }
    }
}