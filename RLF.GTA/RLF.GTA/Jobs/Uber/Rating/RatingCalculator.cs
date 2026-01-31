// ===============================
// RatingCalculator.cs
// ===============================
using System;

namespace RLF.GTA.Jobs.Uber.Rating
{
    public static class RatingCalculator
    {
        public static float CalculateNewAverage(
            float currentAverage,
            int totalRides,
            float newRating)
        {
            if (totalRides <= 0)
                return newRating;

            float total = currentAverage * totalRides;
            total += newRating;
            float newAverage = total / (totalRides + 1);

            return (float)Math.Round(newAverage, 2);
        }

        public static float ApplyPenalty(float currentRating, float penalty)
        {
            float newRating = currentRating - penalty;
            return Math.Max(1.0f, newRating);
        }
    }
}