namespace RLF.GTA.Identity.FlightSchool
{
    /// <summary>
    /// Contexto global de prova de CHT (piloto de avião).
    /// Enquanto ativo, sistemas de lei devem ignorar infrações.
    /// </summary>
    public static class FlightTestContext
    {
        public static bool IsActive { get; private set; }

        public static void Enter()
        {
            IsActive = true;
        }

        public static void Exit()
        {
            IsActive = false;
        }
    }
}