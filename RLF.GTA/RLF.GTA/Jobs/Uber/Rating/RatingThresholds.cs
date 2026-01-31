// ===============================
// RatingThresholds.cs
// ===============================
namespace RLF.GTA.Jobs.Uber.Rating
{
    public static class RatingThresholds
    {
        // Limites de avaliação
        public const float Excellent = 4.8f;   // 4.8 - 5.0
        public const float Good = 4.5f;        // 4.5 - 4.79
        public const float Average = 4.0f;     // 4.0 - 4.49
        public const float Poor = 3.5f;        // 3.5 - 3.99
        public const float Critical = 2.5f;    // < 3.5

        // Benefícios por faixa
        public static float GetTipBonusMultiplier(float rating)
        {
            if (rating >= Excellent)
                return 1.5f; // +50% chance de gorjeta maior
            else if (rating >= Good)
                return 1.2f; // +20%
            else if (rating >= Average)
                return 1.0f; // Normal
            else if (rating >= Poor)
                return 0.8f; // -20%
            else
                return 0.5f; // -50%
        }

        public static float GetRideFrequencyMultiplier(float rating)
        {
            if (rating >= Excellent)
                return 1.3f; // +30% mais corridas
            else if (rating >= Good)
                return 1.1f; // +10%
            else if (rating >= Average)
                return 1.0f; // Normal
            else if (rating >= Poor)
                return 0.8f; // -20%
            else
                return 0.6f; // -40%
        }

        public static bool CanAccessUberBlack(float rating)
        {
            return rating >= Good; // Mínimo 4.5 estrelas
        }

        public static string GetRatingTier(float rating)
        {
            if (rating >= Excellent)
                return "Excelente";
            else if (rating >= Good)
                return "Bom";
            else if (rating >= Average)
                return "Médio";
            else if (rating >= Poor)
                return "Ruim";
            else
                return "Crítico";
        }
    }
}