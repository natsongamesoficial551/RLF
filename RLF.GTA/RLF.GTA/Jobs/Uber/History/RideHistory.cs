// ===============================
// RideHistory.cs
// ===============================
using System.Collections.Generic;
using System.Linq;

namespace RLF.GTA.Jobs.Uber.History
{
    public sealed class RideHistory
    {
        private readonly List<RideRecord> _records;
        private const int MaxRecords = 100;

        public IReadOnlyList<RideRecord> Records => _records;

        public RideHistory()
        {
            _records = new List<RideRecord>();
        }

        public void AddRecord(RideRecord record)
        {
            _records.Add(record);

            // Mantém apenas as últimas 100 corridas
            if (_records.Count > MaxRecords)
            {
                _records.RemoveAt(0);
            }
        }

        public IReadOnlyList<RideRecord> GetRecent(int count)
        {
            return _records
                .OrderByDescending(r => r.Date)
                .Take(count)
                .ToList();
        }

        public void Clear()
        {
            _records.Clear();
        }
    }
}