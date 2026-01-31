// ===============================
// RatingSystem.cs
// ===============================
using System;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Uber.Rating
{
    public sealed class RatingSystem
    {
        private readonly Logger _logger;
        private readonly Random _rng;

        public RatingSystem(Logger logger)
        {
            _logger = logger;
            _rng = new Random();
        }

        public float CalculateRideRating(Ride.RideState ride, int timeExpectedSeconds)
        {
            // Base: 5 estrelas
            float rating = 5.0f;

            // Penalidade por batidas (-0.5 por batida)
            rating -= (ride.CrashCount * 0.5f);

            // Penalidade por direção perigosa (-0.2 por ocorrência)
            rating -= (ride.DangerousDrivingCount * 0.2f);

            // Penalidade por atraso (se demorou 50% a mais)
            if (ride.TimeElapsedSeconds > timeExpectedSeconds * 1.5f)
                rating -= 0.3f;

            // Normaliza entre 1 e 5
            rating = Math.Max(1.0f, Math.Min(5.0f, rating));

            _logger.Info($"[Uber] Avaliação da corrida: {rating:F1} estrelas");
            return rating;
        }
    }
}