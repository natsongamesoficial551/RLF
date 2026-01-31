namespace RLF.GTA.Law.Police
{
    public enum AirPoliceApproachState
    {
        None = 0,

        // Player tem 1 minuto para descer abaixo de 200m
        WarningPhase,

        // Wanted 4 constante enquanto voando
        InterceptionPhase,

        Finished
    }
}