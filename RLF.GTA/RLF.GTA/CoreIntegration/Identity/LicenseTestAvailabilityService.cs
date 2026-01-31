using RLF.Core;
using RLF.Core.Identity.Enums;

namespace RLF.GTA.CoreIntegration.Identity
{
    /// <summary>
    /// Serviço central que decide se um teste de licença
    /// deve estar disponível para o jogador.
    /// 
    /// Usado por:
    /// - CNH
    /// - Porte de Arma
    /// - CHT (futuro)
    /// </summary>
    public static class LicenseTestAvailabilityService
    {
        /// <summary>
        /// Retorna TRUE se o jogador PRECISA fazer o teste.
        /// Retorna FALSE se já possui licença válida.
        /// </summary>
        public static bool ShouldShowTest(LicenseType licenseType)
        {
            var docSystem = RLFCore.Instance?.Systems.Get("DocumentSystem")
                as RLF.Core.Identity.DocumentSystem;

            if (docSystem == null)
                return false; // segurança: não mostra nada se sistema não existir

            // Se já tem licença válida → NÃO mostra teste
            return !docSystem.HasValidLicense(licenseType);
        }
    }
}
