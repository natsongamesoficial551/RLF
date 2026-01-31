namespace RLF.Core.Crime
{
    /// <summary>
    /// Tipos de crimes registráveis no sistema.
    /// Cada tipo tem severidade e consequências diferentes.
    /// </summary>
    public enum CrimeType
    {
        None = 0,

        // Crimes contra pessoa
        PedestrianRobbery = 1,
        WeaponThreat = 2,
        PhysicalAssault = 3,

        // Crimes com arma de fogo
        PublicGunfire = 10,

        // Crimes contra patrimônio
        VehicleTheft = 20,
        VehicleCarjacking = 21,
        StoreRobbery = 22,
        BankRobbery = 23,

        // Crimes de gangue
        GangActivity = 30,
        GangTerritory = 31,

        // Crimes contra autoridade
        DangerousEvasion = 40,
        Resistance = 41,

        // Crimes graves
        Murder = 50,
        MurderWitness = 51
    }
}
