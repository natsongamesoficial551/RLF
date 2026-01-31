using RLF.Core.Economy;

namespace RLF.GTA.CoreIntegration
{
    /// <summary>
    /// Ponte entre scripts GTA e o EconomySystem do Core.
    /// Evita casts inválidos e dependência do SystemRegistry.
    /// </summary>
    public static class EconomyBridge
    {
        /// <summary>
        /// Instância atual do EconomySystem
        /// </summary>
        public static EconomySystem Current { get; internal set; }

        /// <summary>
        /// Indica se o sistema econômico já foi inicializado
        /// </summary>
        public static bool IsReady => Current != null;
    }
}
