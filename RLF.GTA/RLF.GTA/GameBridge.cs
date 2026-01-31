using RLF.Core;

namespace RLF.GTA
{
    /// <summary>
    /// Ponte oficial entre GTA e RLF.Core
    /// </summary>
    public static class GameBridge
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            RLFCore.Instance.Initialize(
                configPath: "scripts/RLF/RLF.ini",
                logDirectory: "scripts/RLF/Logs",
                debugMode: false
            );

            _initialized = true;
        }

        public static void Tick()
        {
            if (!_initialized)
                return;

            // 🔄 Tick do Core
            RLFCore.Instance.Tick();
        }

        public static void Shutdown()
        {
            if (!_initialized)
                return;

            RLFCore.Instance.Shutdown();
            _initialized = false;
        }
    }
}