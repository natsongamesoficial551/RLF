namespace RLF.GTA.GTAOnly.Awareness
{
    /// <summary>
    /// Configurações globais do mod Consciência Situacional (standalone).
    /// </summary>
    public static class AwarenessConfig
    {
        // ===== Sistemas (PASSO 2) =====
        public static bool EnableMovementAwareness = true;
        public static bool EnableStressAwareness = true;
        public static bool EnableFatigueAwareness = true;
        public static bool EnableHeightAwareness = true;

        // Intensidade geral (0.0 = off, 1.0 = normal)
        public static float GlobalIntensity = 1.0f;

        // ===== Camera tuning =====
        public static float MaxFovOffset = 6.0f;     // máximo de +/-
        public static float MaxShake = 0.35f;        // 0–1, mas usamos baixo

        public static float FovSmoothing = 0.14f;    // 0–1 (maior = responde mais rápido)
        public static float ShakeSmoothing = 0.20f;  // 0–1

        // Pesos (como cada sensação afeta a câmera)
        public static float MovementShakeWeight = 0.14f;
        public static float StressShakeWeight = 0.22f;
        public static float FatigueShakeWeight = 0.10f;
        public static float HeightShakeWeight = 0.08f;

        public static float StressFovWeight = -2.0f;   // estresse fecha FOV
        public static float MovementFovWeight = 1.5f;  // corrida abre levemente
        public static float FatigueFovWeight = -1.0f;  // fadiga fecha levemente

        // ===== Audio (opcional/seguro) =====
        public static bool EnableAudio = false; // deixe false por enquanto
        public static float AudioTriggerThreshold = 0.65f;
        public static int AudioCooldownSeconds = 8;

        // Se quiser testar depois, a gente coloca um nome/set que funcione na sua build.
        public static string AudioSoundName = "";
        public static string AudioSoundSet = "";

        // ===== Debug =====
        public static bool DebugNotifications = false;
    }
}
