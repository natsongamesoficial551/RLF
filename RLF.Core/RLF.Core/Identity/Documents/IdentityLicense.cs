using RLF.Core.Identity.Enums;
using System;

namespace RLF.Core.Identity.Documents
{
    public class IdentityLicense : IdentityDocument
    {
        public int Points { get; internal set; }

        public LicenseType LicenseType
        {
            get
            {
                if (Metadata.TryGetValue("LicenseType", out var s) &&
                    Enum.TryParse(s, out LicenseType parsed))
                    return parsed;

                return default(LicenseType);
            }
        }

        public IdentityLicense(LicenseType type)
            : base(DocumentType.IdentityCard)
        {
            Metadata["LicenseType"] = type.ToString();
            Points = 0;
        }
    }
}
