namespace RLF.GTA.Identity.DrivingSchool
{
    /// <summary>
    /// Contexto global de prova de CNH.
    /// Enquanto ativo, sistemas de lei devem ignorar infrações.
    /// </summary>
    public static class DrivingTestContext
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
