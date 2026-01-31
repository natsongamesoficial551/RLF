using RLF.Core;
using RLF.GTA.Entities;
using RLF.GTA.Performance;

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

            // Inicializa ponte de entidades
            if (RLFCore.Instance.Entities != null)
            {
                GTAEntityBridge.Initialize(RLFCore.Instance.Entities);
            }

            _initialized = true;
        }

        public static void Tick()
        {
            if (!_initialized)
                return;

            // 📸 Captura snapshot do player
            PlayerSnapshotCapture.Instance.Capture();

            // 📍 Atualiza posição no EntityRegistry
            GTAEntityBridge.UpdatePlayerPosition();

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

        /// <summary>
        /// Acesso rápido ao snapshot do player.
        /// </summary>
        public static Core.Performance.PlayerSnapshot PlayerSnapshot
            => PlayerSnapshotCapture.Instance.Current;
    }
}