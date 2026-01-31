// ===============================
// UberAccount.cs
// ===============================
using System;

namespace RLF.GTA.Jobs.Uber.Core
{
    public sealed class UberAccount
    {
        public float AverageRating { get; internal set; }
        public int TotalRides { get; internal set; }
        public decimal TotalEarned { get; internal set; }
        public int CancellationCount { get; internal set; }
        public DateTime? BannedUntil { get; internal set; }
        public bool IsBanned => BannedUntil.HasValue && DateTime.UtcNow < BannedUntil.Value;

        public UberAccount()
        {
            AverageRating = 5.0f;
            TotalRides = 0;
            TotalEarned = 0m;
            CancellationCount = 0;
            BannedUntil = null;
        }

        public void RecordRide(float rating, decimal payment)
        {
            AverageRating = Rating.RatingCalculator.CalculateNewAverage(
                AverageRating,
                TotalRides,
                rating
            );

            TotalRides++;
            TotalEarned += payment;
        }

        public void RecordCancellation()
        {
            CancellationCount++;
        }

        public void ApplyRatingPenalty(float penalty)
        {
            AverageRating = Rating.RatingCalculator.ApplyPenalty(AverageRating, penalty);
        }

        public void SetBan(DateTime until)
        {
            BannedUntil = until;
        }

        public void ClearBan()
        {
            BannedUntil = null;
        }

        public void ResetCancellationCount()
        {
            CancellationCount = 0;
        }
    }
}