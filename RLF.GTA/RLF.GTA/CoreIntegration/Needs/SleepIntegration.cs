using RLF.Core.Needs;

namespace RLF.GTA.CoreIntegration.Needs
{
    /// <summary>
    /// Integração de sono.
    /// O sono NÃO é detectado automaticamente.
    /// Ele deve ser acionado explicitamente por:
    /// - sistema de moradia
    /// - sistema de save
    /// - cama/interior no futuro
    /// </summary>
    public class SleepIntegration
    {
        private readonly NeedsSystem _needs;

        public SleepIntegration(NeedsSystem needs)
        {
            _needs = needs;
        }

        /// <summary>
        /// Deve ser chamado quando o jogador dormir de fato.
        /// </summary>
        public void PlayerSlept(float hours)
        {
            if (hours <= 0f)
                return;

            _needs.Sleep(hours);
        }

        // Tick vazio propositalmente
        public void Tick()
        {
        }
    }
}
