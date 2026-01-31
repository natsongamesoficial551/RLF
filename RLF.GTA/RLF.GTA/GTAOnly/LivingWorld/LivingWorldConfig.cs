namespace RLF.GTA.GTAOnly.LivingWorld
{
    public static class LivingWorldConfig
    {
        // ===============================
        // 🌍 MODO REALISTA
        // ===============================

        // Apenas 1 evento ativo (mais realista e seguro)
        public static int MaxActiveScenes = 1;

        // Tempo médio entre eventos (1.5 a 3 min na prática)
        public static float SpawnCooldownSeconds = 120f;

        // Distância segura
        public static float MinSpawnDistance = 60f;
        public static float MaxSpawnDistance = 120f;

        // Chance global de tentar spawnar
        // (não é 100% toda tentativa)
        public static float SpawnChance = 0.65f;

        // Visual / Debug
        public static bool NotifyOnSpawn = false; // mais realista
        public static bool CreateBlipForEvents = false; // pode desligar depois

        // Segurança
        public static float MinDistanceFromPlayerSafety = 35f;
        public static int BlipMaxSeconds = 90;
    }
}
