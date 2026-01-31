using RLF.Core.Configuration;
using RLF.Core.Identity.Documents;
using RLF.Core.Identity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Identity.Storage
{
    public class IniIdentityStore : IIdentityStore
    {
        private readonly IniReader _ini;

        public IniIdentityStore(string path)
        {
            _ini = new IniReader(path);
            _ini.Load();
        }

        public IEnumerable<IdentityDocument> Load()
        {
            var docs = new List<IdentityDocument>();

            // ✅ CARREGA LICENÇAS (CNH, CHT, Porte, etc)
            foreach (LicenseType licType in Enum.GetValues(typeof(LicenseType)))
            {
                string section = "License_" + licType.ToString();

                var status = (DocumentStatus)_ini.GetInt(section, "Status", 0);

                // Só adiciona se foi emitida (não Missing)
                if (status != DocumentStatus.Missing)
                {
                    var lic = new IdentityLicense(licType)
                    {
                        Status = status,
                        IssuedAt = ParseDateTime(_ini.GetString(section, "IssuedAt", "")),
                        ExpiresAt = ParseDateTimeNullable(_ini.GetString(section, "ExpiresAt", "")),
                        LastStatusChangeAt = ParseDateTime(_ini.GetString(section, "LastStatusChangeAt", "")),
                        Reason = _ini.GetString(section, "Reason", "")
                    };

                    docs.Add(lic);
                }
            }

            return docs;
        }

        public void Save(IEnumerable<IdentityDocument> documents)
        {
            // ✅ SALVA APENAS LICENÇAS (ignorando documentos genéricos)
            var licenses = documents.OfType<IdentityLicense>();

            foreach (var lic in licenses)
            {
                string section = "License_" + lic.LicenseType.ToString();

                _ini.SetInt(section, "Status", (int)lic.Status);
                _ini.SetString(section, "IssuedAt", lic.IssuedAt.ToString("o"));
                _ini.SetString(section, "ExpiresAt", lic.ExpiresAt?.ToString("o") ?? "");
                _ini.SetString(section, "LastStatusChangeAt", lic.LastStatusChangeAt.ToString("o"));
                _ini.SetString(section, "Reason", lic.Reason ?? "");
            }

            _ini.Save();
        }

        // ===============================
        // 🛠️ HELPERS
        // ===============================
        private DateTime ParseDateTime(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return DateTime.UtcNow;

            if (DateTime.TryParse(s, out var dt))
                return dt;

            return DateTime.UtcNow;
        }

        private DateTime? ParseDateTimeNullable(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (DateTime.TryParse(s, out var dt))
                return dt;

            return null;
        }
    }
}