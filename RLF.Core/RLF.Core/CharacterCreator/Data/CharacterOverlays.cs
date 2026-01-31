using RLF.Core.CharacterCreator.Enums;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class CharacterOverlay
    {
        [XmlElement]
        public OverlayType Type { get; set; }

        [XmlElement]
        public int Index { get; set; }

        [XmlElement]
        public float Opacity { get; set; }

        [XmlElement]
        public int PrimaryColor { get; set; }

        [XmlElement]
        public int SecondaryColor { get; set; }

        public CharacterOverlay()
        {
            Type = OverlayType.Blemishes;
            Index = -1;
            Opacity = 1f;
            PrimaryColor = 0;
            SecondaryColor = 0;
        }

        public CharacterOverlay(OverlayType type, int index, float opacity, int primaryColor = 0, int secondaryColor = 0)
        {
            Type = type;
            Index = index;
            Opacity = opacity;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
        }

        public CharacterOverlay Clone()
        {
            return new CharacterOverlay(Type, Index, Opacity, PrimaryColor, SecondaryColor);
        }
    }

    [Serializable]
    public class CharacterOverlays
    {
        [XmlArray("Overlays")]
        [XmlArrayItem("Overlay")]
        public List<CharacterOverlay> Overlays { get; set; }

        public CharacterOverlays()
        {
            Overlays = new List<CharacterOverlay>();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Inicializar todos os overlays com -1 (desativado)
            foreach (OverlayType type in Enum.GetValues(typeof(OverlayType)))
            {
                Overlays.Add(new CharacterOverlay(type, -1, 1f, 0, 0));
            }
        }

        public void SetOverlay(OverlayType type, int index, float opacity, int primaryColor = 0, int secondaryColor = 0)
        {
            var existing = Overlays.Find(o => o.Type == type);
            if (existing != null)
            {
                existing.Index = index;
                existing.Opacity = opacity;
                existing.PrimaryColor = primaryColor;
                existing.SecondaryColor = secondaryColor;
            }
            else
            {
                Overlays.Add(new CharacterOverlay(type, index, opacity, primaryColor, secondaryColor));
            }
        }

        public CharacterOverlay GetOverlay(OverlayType type)
        {
            return Overlays.Find(o => o.Type == type);
        }

        public void ClearOverlay(OverlayType type)
        {
            SetOverlay(type, -1, 1f, 0, 0);
        }

        public void ClearAllOverlays()
        {
            foreach (var overlay in Overlays)
            {
                overlay.Index = -1;
                overlay.Opacity = 1f;
                overlay.PrimaryColor = 0;
                overlay.SecondaryColor = 0;
            }
        }

        public CharacterOverlays Clone()
        {
            var clone = new CharacterOverlays();
            clone.Overlays.Clear();

            foreach (var overlay in Overlays)
            {
                clone.Overlays.Add(overlay.Clone());
            }

            return clone;
        }
    }
}