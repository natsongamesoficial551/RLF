using RLF.Core.Configuration;

namespace RLF.Core.Needs
{
    public static class NeedsSettingsLoader
    {
        public static NeedsSettings Load()
        {
            var ini = new IniReader("scripts/RLF/needs.ini");
            ini.Load();

            return new NeedsSettings
            {
                // ===== MAX =====
                MaxHunger = ini.GetFloat("Max", "Hunger", 100f),
                MaxThirst = ini.GetFloat("Max", "Thirst", 100f),
                MaxSleep = ini.GetFloat("Max", "Sleep", 100f),
                MaxStamina = ini.GetFloat("Max", "Stamina", 100f),

                // ===== INITIAL =====
                InitialHunger = ini.GetFloat("Initial", "Hunger", 100f),
                InitialThirst = ini.GetFloat("Initial", "Thirst", 100f),
                InitialSleep = ini.GetFloat("Initial", "Sleep", 100f),
                InitialStamina = ini.GetFloat("Initial", "Stamina", 100f),

                // ===== DECAY (per hour) =====
                HungerDecayPerHour = ini.GetFloat("Decay", "HungerPerHour", 3.5f),
                ThirstDecayPerHour = ini.GetFloat("Decay", "ThirstPerHour", 5.0f),
                SleepDecayPerHour = ini.GetFloat("Decay", "SleepPerHour", 2.0f),

                // ===== SLEEP RESTORE =====
                SleepRestorePerHour = ini.GetFloat("Sleep", "RestorePerHour", 15f),

                // ===== STAMINA =====
                StaminaDrainMultiplier = ini.GetFloat("Stamina", "DrainMultiplier", 1.0f),
                StaminaRegenMultiplier = ini.GetFloat("Stamina", "RegenMultiplier", 1.0f),

                // ===== THRESHOLDS =====
                WarningThreshold = ini.GetFloat("Thresholds", "Warning", 60f),
                CriticalThreshold = ini.GetFloat("Thresholds", "Critical", 30f),
            };
        }
    }
}
