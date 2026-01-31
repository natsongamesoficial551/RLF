using GTA.UI;
using RLF.Core;
using RLF.Core.Needs;
using System;
using System.Drawing;

namespace RLF.GTA.CoreIntegration.UI
{
    public class NeedsOverlay
    {
        private NeedsSystem _needs;
        private readonly NeedsOverlaySettings _settings;

        private const char BarFull = '█';
        private const char BarEmpty = '░';

        public NeedsOverlay()
        {
            _settings = NeedsOverlayConfigLoader.Load();
        }

        public void Draw()
        {
            if (_needs == null)
            {
                _needs = RLFCore.Instance.Systems.Get("NeedsSystem") as NeedsSystem;
                if (_needs == null) return;
            }

            DrawNeed(NeedType.Hunger, "H", 0);
            DrawNeed(NeedType.Thirst, "T", 1);
            DrawNeed(NeedType.Sleep, "S", 2);
            DrawNeed(NeedType.Stamina, "ST", 3);
        }

        private void DrawNeed(NeedType type, string label, int index)
        {
            float value = _needs.GetNeedValue(type);

            int barLength = _settings.BarLength <= 0 ? 10 : _settings.BarLength;

            int filledBars = (int)Math.Round((value / 100f) * barLength);
            filledBars = Math.Max(0, Math.Min(barLength, filledBars));

            string bar =
                new string(BarFull, filledBars) +
                new string(BarEmpty, barLength - filledBars);

            Color color = GetColor(value);

            int x = _settings.Position.X;
            int y = _settings.Position.Y + (index * _settings.LineHeight);

            new TextElement(label, new Point(x, y), 0.32f, color).Draw();
            new TextElement(bar, new Point(x + 32, y), 0.30f, color).Draw();
            new TextElement(value.ToString("0.0"), new Point(x + 160, y), 0.30f, color).Draw();
        }

        private Color GetColor(float value)
        {
            if (value <= _settings.CriticalThreshold)
                return _settings.CriticalColor;

            if (value <= _settings.WarningThreshold)
                return _settings.WarningColor;

            return _settings.NormalColor;
        }
    }
}
