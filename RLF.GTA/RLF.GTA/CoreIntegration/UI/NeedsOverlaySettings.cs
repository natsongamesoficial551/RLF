using System.Drawing;

namespace RLF.GTA.CoreIntegration.UI
{
    public class NeedsOverlaySettings
    {
        public Point Position { get; set; } = new Point(20, 80);
        public int LineHeight { get; set; } = 26;
        public int BarLength { get; set; } = 10;

        public Color NormalColor { get; set; } = Color.White;
        public Color WarningColor { get; set; } = Color.FromArgb(255, 200, 80);
        public Color CriticalColor { get; set; } = Color.FromArgb(220, 80, 80);

        public float WarningThreshold { get; set; } = 60f;
        public float CriticalThreshold { get; set; } = 30f;
    }
}
