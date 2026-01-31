namespace RLF.Core.Safety
{
    /// <summary>
    /// Interface que o RLF.GTA implementa para fornecer dados ao Core.
    /// O Core NUNCA acessa GTA diretamente - apenas recebe dados por aqui.
    /// </summary>
    public interface ISafetyDataProvider
    {
        // ========== PLAYER DATA ==========
        float GetPlayerPositionX();
        float GetPlayerPositionY();
        float GetPlayerPositionZ();
        float GetPlayerSpeed();
        float GetPlayerHealth();
        bool IsPlayerInVehicle();
        bool IsPlayerInCover();
        bool IsPlayerInCombat();
        int GetPlayerWantedLevel();

        // ========== INPUT DATA ==========
        bool IsAnyMovementInputPressed();
        bool IsAttackInputPressed();
        bool IsAimInputPressed();

        // ========== GAME STATE ==========
        bool IsGamePaused();
        bool IsCutsceneActive();
        bool IsInteriorScene();
        int GetGameHour();
        int GetCurrentWeather(); // Enum convertido para int

        // ========== WORLD DATA (chamado raramente) ==========
        int GetNearbyPedCount(float radius);
        int GetNearbyVehicleCount(float radius);

        // ========== PERFORMANCE ==========
        float GetLastFrameTime();
    }
}