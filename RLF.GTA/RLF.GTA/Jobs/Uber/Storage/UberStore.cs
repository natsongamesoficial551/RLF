// ===============================
// UberStore.cs
// ===============================
using System;
using System.Linq;
using RLF.Core.Configuration;
using RLF.GTA.Jobs.Uber.Core;
using RLF.GTA.Jobs.Uber.History;
using RLF.GTA.Jobs.Uber.Ride;

namespace RLF.GTA.Jobs.Uber.Storage
{
    public sealed class UberStore : IUberStore
    {
        private readonly IniReader _ini;

        public UberStore(string path)
        {
            _ini = new IniReader(path);
            _ini.Load();
        }

        public void Save(UberAccount account, RideHistory history)
        {
            _ini.SetFloat("Account", "AverageRating", account.AverageRating);
            _ini.SetInt("Account", "TotalRides", Math.Min(account.TotalRides, 999999));
            _ini.SetString("Account", "TotalEarned", Math.Round(account.TotalEarned, 2).ToString("F2"));
            _ini.SetInt("Account", "CancellationCount", Math.Min(account.CancellationCount, 999));
            _ini.SetString("Account", "BannedUntil", account.BannedUntil?.ToString("o") ?? "");

            var recent = history.GetRecent(20).ToList();
            _ini.SetInt("History", "Count", recent.Count);

            for (int i = 0; i < recent.Count; i++)
            {
                string section = $"Ride_{i}";
                var record = recent[i];

                _ini.SetString(section, "Date", record.Date.ToString("o"));
                _ini.SetInt(section, "Category", (int)record.Category);
                _ini.SetString(section, "Origin", record.Origin ?? "");
                _ini.SetString(section, "Destination", record.Destination ?? "");
                _ini.SetFloat(section, "Distance", Math.Max(0f, Math.Min(record.Distance, 999999f)));
                _ini.SetString(section, "Payment", Math.Round(record.Payment, 2).ToString("F2"));
                _ini.SetString(section, "Tip", Math.Round(record.Tip, 2).ToString("F2"));
                _ini.SetFloat(section, "Rating", Math.Max(1f, Math.Min(record.Rating, 5f)));
                _ini.SetString(section, "Events", record.Events ?? "");
            }

            _ini.Save();
        }

        public (UberAccount account, RideHistory history) Load()
        {
            var account = new UberAccount
            {
                AverageRating = _ini.GetFloat("Account", "AverageRating", 5.0f),
                TotalRides = _ini.GetInt("Account", "TotalRides", 0),
                TotalEarned = ParseDecimal(_ini.GetString("Account", "TotalEarned", "0")),
                CancellationCount = _ini.GetInt("Account", "CancellationCount", 0)
            };

            string bannedStr = _ini.GetString("Account", "BannedUntil", "");
            if (!string.IsNullOrWhiteSpace(bannedStr) && DateTime.TryParse(bannedStr, out var banned))
            {
                account.BannedUntil = banned;
            }

            var history = new RideHistory();
            int count = _ini.GetInt("History", "Count", 0);

            for (int i = 0; i < count; i++)
            {
                string section = $"Ride_{i}";

                var record = new RideRecord
                {
                    Date = ParseDateTime(_ini.GetString(section, "Date", "")),
                    Category = (RideCategory)_ini.GetInt(section, "Category", 0),
                    Origin = _ini.GetString(section, "Origin", ""),
                    Destination = _ini.GetString(section, "Destination", ""),
                    Distance = _ini.GetFloat(section, "Distance", 0f),
                    Payment = ParseDecimal(_ini.GetString(section, "Payment", "0")),
                    Tip = ParseDecimal(_ini.GetString(section, "Tip", "0")),
                    Rating = _ini.GetFloat(section, "Rating", 0f),
                    Events = _ini.GetString(section, "Events", "")
                };

                history.AddRecord(record);
            }

            return (account, history);
        }

        private decimal ParseDecimal(string s)
        {
            if (decimal.TryParse(s, out var result))
                return result;
            return 0m;
        }

        private DateTime ParseDateTime(string s)
        {
            if (DateTime.TryParse(s, out var result))
                return result;
            return DateTime.UtcNow;
        }
    }
}