// ===============================
// HistoryManager.cs
// ===============================
using System;
using System.Linq;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Uber.History
{
    public sealed class HistoryManager
    {
        private readonly RideHistory _history;
        private readonly Logger _logger;

        public RideHistory History => _history;

        public HistoryManager(RideHistory history, Logger logger)
        {
            _history = history ?? new RideHistory();
            _logger = logger;
        }

        public void RecordRide(
            Ride.RideCategory category,
            float distance,
            decimal payment,
            decimal tip,
            float rating,
            string events)
        {
            var record = new RideRecord
            {
                Date = DateTime.UtcNow,
                Category = category,
                Origin = "Ponto de Coleta",
                Destination = "Destino",
                Distance = distance,
                Payment = payment,
                Tip = tip,
                Rating = rating,
                Events = events ?? string.Empty
            };

            _history.AddRecord(record);
            _logger.Info($"[Uber] Corrida registrada no histórico: {category} - ${payment + tip:F2}");
        }

        public decimal GetTotalEarnings()
        {
            return _history.Records.Sum(r => r.Payment + r.Tip);
        }

        public float GetAverageRating()
        {
            if (_history.Records.Count == 0)
                return 5.0f;

            return _history.Records.Average(r => r.Rating);
        }

        public int GetTotalRides()
        {
            return _history.Records.Count;
        }

        public RideRecord GetBestRide()
        {
            if (_history.Records.Count == 0)
                return null;

            return _history.Records.OrderByDescending(r => r.Payment + r.Tip).FirstOrDefault();
        }

        public RideRecord GetWorstRide()
        {
            if (_history.Records.Count == 0)
                return null;

            return _history.Records.OrderBy(r => r.Rating).FirstOrDefault();
        }
    }
}