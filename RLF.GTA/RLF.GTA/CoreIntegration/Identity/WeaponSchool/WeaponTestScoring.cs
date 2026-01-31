using System;

namespace RLF.GTA.CoreIntegration.Identity.WeaponSchool
{
    public enum WeaponTestResult
    {
        Failed,
        Medium,
        Good
    }

    public static class WeaponTestScoring
    {
        // ===============================
        // CONFIGURAÇÃO DE BALANCEAMENTO
        // ===============================
        private const float GOOD_ACCURACY = 0.70f;
        private const float MEDIUM_ACCURACY = 0.45f;

        private const float MAX_TIME_PENALTY = 90f;

        // ===============================
        // AVALIAÇÃO
        // ===============================
        public static WeaponTestResult Evaluate(
            int shotsFired,
            int shotsHit,
            float elapsedSeconds)
        {
            if (shotsFired <= 0 || shotsHit <= 0)
                return WeaponTestResult.Failed;

            float accuracy = (float)shotsHit / shotsFired;

            // Penalidade por demora excessiva
            if (elapsedSeconds > MAX_TIME_PENALTY)
            {
                accuracy -= 0.10f;
            }

            accuracy = Math.Max(0f, accuracy);

            if (accuracy >= GOOD_ACCURACY)
                return WeaponTestResult.Good;

            if (accuracy >= MEDIUM_ACCURACY)
                return WeaponTestResult.Medium;

            return WeaponTestResult.Failed;
        }
    }
}
