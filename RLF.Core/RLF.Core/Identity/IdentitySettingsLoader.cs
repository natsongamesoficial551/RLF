using RLF.Core.Configuration;

namespace RLF.Core.Identity
{
    public static class IdentitySettingsLoader
    {
        public static IdentitySettings Load()
        {
            var ini = new IniReader("scripts/RLF/identity.ini");
            ini.Load();

            return new IdentitySettings
            {
                DefaultLicenseValidityDays = ini.GetInt("General", "DefaultLicenseValidityDays", 365),
                MaxDriverLicensePoints = ini.GetInt("General", "MaxDriverLicensePoints", 20)
            };
        }
    }
}
