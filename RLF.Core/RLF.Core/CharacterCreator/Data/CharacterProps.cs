using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class PropItem
    {
        [XmlElement]
        public PropType Type { get; set; }

        [XmlElement]
        public int DrawableId { get; set; }

        [XmlElement]
        public int TextureId { get; set; }

        public PropItem()
        {
            Type = PropType.Hat;
            DrawableId = -1;
            TextureId = 0;
        }

        public PropItem(PropType type, int drawable, int texture)
        {
            Type = type;
            DrawableId = drawable;
            TextureId = texture;
        }

        public PropItem Clone()
        {
            return new PropItem(Type, DrawableId, TextureId);
        }
    }

    [Serializable]
    public class CharacterProps
    {
        [XmlArray("Props")]
        [XmlArrayItem("Prop")]
        public List<PropItem> Props { get; set; }

        public CharacterProps()
        {
            Props = new List<PropItem>();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            foreach (PropType type in Enum.GetValues(typeof(PropType)))
            {
                Props.Add(new PropItem(type, -1, 0));
            }
        }

        public void SetProp(PropType type, int drawable, int texture)
        {
            var existing = Props.Find(p => p.Type == type);
            if (existing != null)
            {
                existing.DrawableId = drawable;
                existing.TextureId = texture;
            }
            else
            {
                Props.Add(new PropItem(type, drawable, texture));
            }
        }

        public PropItem GetProp(PropType type)
        {
            return Props.Find(p => p.Type == type);
        }

        public int GetDrawable(PropType type)
        {
            var item = GetProp(type);
            return item?.DrawableId ?? -1;
        }

        public int GetTexture(PropType type)
        {
            var item = GetProp(type);
            return item?.TextureId ?? 0;
        }

        public void ClearProp(PropType type)
        {
            SetProp(type, -1, 0);
        }

        public void ClearAllProps()
        {
            foreach (var prop in Props)
            {
                prop.DrawableId = -1;
                prop.TextureId = 0;
            }
        }

        public CharacterProps Clone()
        {
            var clone = new CharacterProps();
            clone.Props.Clear();

            foreach (var item in Props)
            {
                clone.Props.Add(item.Clone());
            }

            return clone;
        }
    }
}