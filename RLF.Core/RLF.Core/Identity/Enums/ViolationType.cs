namespace RLF.Core.Identity.Enums
{
    public enum ViolationType
    {
        DrivingWithoutLicense,
        ExpiredDriverLicense,
        SuspendedDriverLicense,
        WeaponWithoutPermit,

        // ✅ Novo (necessário pro AircraftLicenseObserver)
        FlyingWithoutLicense
    }
}
