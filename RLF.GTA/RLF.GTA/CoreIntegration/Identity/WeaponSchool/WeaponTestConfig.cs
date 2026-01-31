using GTA;

namespace RLF.GTA.CoreIntegration.Identity.WeaponSchool
{
    /// <summary>
    /// Configurações globais do teste de porte de arma.
    /// Centraliza balanceamento e dificuldade.
    /// </summary>
    public static class WeaponTestConfig
    {
        // ===============================
        // ARMA DO TESTE
        // ===============================
        public static readonly WeaponHash TestWeapon = WeaponHash.Pistol;

        public const int AmmoGiven = 60;

        // ===============================
        // TEMPO
        // ===============================
        public const float MaxTestTimeSeconds = 90f;

        // ===============================
        // ALVOS
        // ===============================
        public const int TargetCount = 5;

        public const float TargetDistance = 8f;
        public const float TargetSpacing = 1.5f;

        // ===============================
        // SEGURANÇA
        // ===============================
        public const bool RemovePlayerWeaponsOnStart = true;
        public const bool RestoreWeaponsAfterTest = false;
    }
}
