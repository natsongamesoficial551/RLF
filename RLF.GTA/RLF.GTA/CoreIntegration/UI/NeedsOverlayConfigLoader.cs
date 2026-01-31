using RLF.Core.Configuration;
using System.Drawing;

namespace RLF.GTA.CoreIntegration.UI
{
    public static class NeedsOverlayConfigLoader
    {
        public static NeedsOverlaySettings Load()
        {
            var ini = new IniReader("scripts/RLF/needs.ini");
            ini.Load();

            return new NeedsOverlaySettings
            {
                Position = new Point(
                    ini.GetInt("HUD", "PosX", 20),
                    ini.GetInt("HUD", "PosY", 80)
                ),

                LineHeight = ini.GetInt("HUD", "LineHeight", 26),
                BarLength = ini.GetInt("HUD", "BarLength", 10),

                NormalColor = Color.FromArgb(
                    ini.GetInt("Colors", "NormalR", 255),
                    ini.GetInt("Colors", "NormalG", 255),
                    ini.GetInt("Colors", "NormalB", 255)
                ),

                WarningColor = Color.FromArgb(
                    ini.GetInt("Colors", "WarningR", 255),
                    ini.GetInt("Colors", "WarningG", 200),
                    ini.GetInt("Colors", "WarningB", 80)
                ),

                CriticalColor = Color.FromArgb(
                    ini.GetInt("Colors", "CriticalR", 220),
                    ini.GetInt("Colors", "CriticalG", 80),
                    ini.GetInt("Colors", "CriticalB", 80)
                ),

                WarningThreshold = ini.GetFloat("Thresholds", "Warning", 60f),
                CriticalThreshold = ini.GetFloat("Thresholds", "Critical", 30f)
            };
        }
    }
}
