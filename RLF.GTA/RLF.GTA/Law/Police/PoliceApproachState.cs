namespace RLF.GTA.Law.Police
{
    public enum PoliceApproachState
    {
        None = 0,

        Following,
        SignalingStop,
        WaitingStop,

        OfficerExit,
        OfficerApproach,
        OfficerInspection,

        VehicleTaken,

        Cleanup,
        Finished
    }
}