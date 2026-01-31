namespace RLF.Core.Needs
{
    /// <summary>
    /// Configurações do sistema de necessidades (Core).
    /// Controla o balanceamento via INI.
    /// </summary>
    public class NeedsSettings
    {
        // =========================
        // Valores máximos
        // =========================
        public float MaxHunger { get; set; } = 100f;
        public float MaxThirst { get; set; } = 100f;
        public float MaxSleep { get; set; } = 100f;
        public float MaxStamina { get; set; } = 100f;

        // =========================
        // Valores iniciais
        // =========================
        public float InitialHunger { get; set; } = 100f;
        public float InitialThirst { get; set; } = 100f;
        public float InitialSleep { get; set; } = 100f;
        public float InitialStamina { get; set; } = 100f;

        // =========================
        // Decay por hora (tempo real)
        // =========================
        public float HungerDecayPerHour { get; set; } = 3.5f;
        public float ThirstDecayPerHour { get; set; } = 5.0f;
        public float SleepDecayPerHour { get; set; } = 2.0f;

        // =========================
        // Sono: recuperação por hora dormida
        // =========================
        public float SleepRestorePerHour { get; set; } = 15f;

        // =========================
        // Multiplicadores de stamina (Core expõe / GTA aplica)
        // =========================
        public float StaminaDrainMultiplier { get; set; } = 1.0f;
        public float StaminaRegenMultiplier { get; set; } = 1.0f;

        // =========================
        // Thresholds (HUD / warnings / feedback)
        // =========================
        public float WarningThreshold { get; set; } = 60f;
        public float CriticalThreshold { get; set; } = 30f;
    }
}
