// ===============================
// RidePaymentCalculator.cs
// ===============================
using System;
using RLF.GTA.Jobs.Uber.Core;

namespace RLF.GTA.Jobs.Uber.Ride
{
    public static class RidePaymentCalculator
    {
        public static decimal CalculatePayment(
    RideState ride,
    UberSettings settings)
        {
            if (!ride.IsActive && ride.DistanceTraveled <= 0)
                return 0m;

            float distanceKm = ride.DistanceTraveled / 1000f;
            float timeMinutes = ride.TimeElapsedSeconds / 60f;

            if (float.IsNaN(distanceKm) || float.IsInfinity(distanceKm))
                distanceKm = 0f;

            if (float.IsNaN(timeMinutes) || float.IsInfinity(timeMinutes))
                timeMinutes = 0f;

            decimal distanceDecimal = (decimal)Math.Round(distanceKm, 2);
            decimal timeDecimal = (decimal)Math.Round(timeMinutes, 2);

            decimal basePay =
                (distanceDecimal * settings.BasePayPerKm) +
                (timeDecimal * settings.BasePayPerMinute);

            decimal multiplier = GetCategoryMultiplier(ride.Category, settings);
            decimal total = basePay * multiplier;

            if (total < settings.MinimumFare)
                total = settings.MinimumFare;

            return Math.Round(total, 2);
        }

        public static decimal CalculateTip(
    RideState ride,
    float rating,
    UberSettings settings,
    Random rng)
        {
            float tipChance = settings.TipChanceBase;

            if (rating >= 4.0f)
                tipChance = settings.TipChanceHigh;

            tipChance -= (ride.CrashCount * 0.15f);
            tipChance -= (ride.DangerousDrivingCount * 0.1f);

            tipChance = Math.Max(0f, Math.Min(1f, tipChance));

            if (rng.NextDouble() > tipChance)
                return 0m;

            double tipRandom = rng.NextDouble();
            decimal tipRange = settings.TipAmountMax - settings.TipAmountMin;

            decimal tipAmount = settings.TipAmountMin + ((decimal)tipRandom * tipRange);

            return Math.Round(tipAmount, 2);
        }

        private static decimal GetCategoryMultiplier(
            RideCategory category,
            UberSettings settings)
        {
            switch (category)
            {
                case RideCategory.UberX:
                    return settings.UberXMultiplier;
                case RideCategory.UberBlack:
                    return settings.UberBlackMultiplier;
                case RideCategory.UberPool:
                    return settings.UberPoolMultiplier;
                default:
                    return 1.0m;
            }
        }
    }
}