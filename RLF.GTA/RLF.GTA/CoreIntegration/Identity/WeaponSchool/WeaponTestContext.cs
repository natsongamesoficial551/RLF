namespace RLF.GTA.CoreIntegration.Identity.WeaponSchool
{
    /// <summary>
    /// Contexto global do teste de porte de arma.
    /// Enquanto ativo:
    /// - Sistema de lei ignora infrações
    /// - WeaponLicenseObserver não dispara violação
    /// </summary>
    public static class WeaponTestContext
    {
        /// <summary>
        /// Indica se o teste de porte está ativo no momento.
        /// </summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        /// Ativa o contexto do teste de porte de arma.
        /// Deve ser chamado ao iniciar o teste.
        /// </summary>
        public static void Enter()
        {
            IsActive = true;
        }

        /// <summary>
        /// Desativa o contexto do teste de porte de arma.
        /// Deve ser chamado ao finalizar (passou ou reprovou).
        /// </summary>
        public static void Exit()
        {
            IsActive = false;
        }
    }
}
