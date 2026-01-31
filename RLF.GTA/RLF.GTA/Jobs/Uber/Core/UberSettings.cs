// ===============================
// UberSettings.cs
// ===============================
namespace RLF.GTA.Jobs.Uber.Core
{
    public sealed class UberSettings
    {
        // Pagamento Base
        public decimal BasePayPerKm { get; set; } = 2.5m;
        public decimal BasePayPerMinute { get; set; } = 0.8m;
        public decimal MinimumFare { get; set; } = 10m;

        // Multiplicadores por Categoria
        public decimal UberXMultiplier { get; set; } = 1.0m;
        public decimal UberBlackMultiplier { get; set; } = 2.5m;
        public decimal UberPoolMultiplier { get; set; } = 0.7m;

        // Gorjetas
        public float TipChanceBase { get; set; } = 0.4f;
        public float TipChanceHigh { get; set; } = 0.7f;
        public decimal TipAmountMin { get; set; } = 5m;
        public decimal TipAmountMax { get; set; } = 30m;

        // Penalidades
        public decimal CancellationPenalty { get; set; } = 15m;
        public float CancellationRatingLoss { get; set; } = 0.2f;

        // Banimento
        public int MaxCancellationsBeforeBan { get; set; } = 3;
        public float MinRatingBeforeBan { get; set; } = 2.5f;

        // Timers
        public int RideRequestTimeoutSeconds { get; set; } = 15;
        public int NewRideDelaySeconds { get; set; } = 5;
    }
}