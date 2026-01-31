// ===============================
// PenaltySystem.cs
// ===============================
using System;
using RLF.Core.Logging;
using RLF.GTA.Jobs.Uber.Core;

namespace RLF.GTA.Jobs.Uber.Penalty
{
    public sealed class PenaltySystem
    {
        private readonly Logger _logger;
        private readonly UberSettings _settings;

        public PenaltySystem(Logger logger, UberSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public bool ShouldBanForCancellations(UberAccount account)
        {
            return account.CancellationCount >= _settings.MaxCancellationsBeforeBan;
        }

        public bool ShouldBanForLowRating(UberAccount account)
        {
            return account.TotalRides >= 5 &&
                   account.AverageRating < _settings.MinRatingBeforeBan;
        }

        public void ApplyBan(UberAccount account, BanType type)
        {
            DateTime banUntil = CalculateBanDuration(type);
            account.SetBan(banUntil);

            _logger.Warning($"[Uber] Conta banida até {banUntil:dd/MM/yyyy HH:mm} ({type})");
        }

        public void ApplyCancellationPenalty(
            UberAccount account,
            RLF.Core.Economy.EconomySystem economy)
        {
            account.RecordCancellation();
            account.ApplyRatingPenalty(_settings.CancellationRatingLoss);

            var transaction = new RLF.Core.Economy.Transactions.EconomyTransaction(
                -_settings.CancellationPenalty,
                RLF.Core.Economy.Transactions.TransactionType.Fine,
                RLF.Core.Economy.Transactions.TransactionLegality.Legal,
                RLF.Core.Economy.Transactions.TransactionOrigin.Fine,
                "Penalidade por cancelamento de corrida Uber"
            );

            economy.ApplyTransaction(transaction);

            _logger.Warning($"[Uber] Penalidade aplicada: ${_settings.CancellationPenalty}");
        }

        public string GetBanMessage(UberAccount account)
        {
            if (!account.IsBanned)
                return string.Empty;

            TimeSpan remaining = account.BannedUntil.Value - DateTime.UtcNow;

            if (remaining.TotalHours >= 1)
            {
                return $"Sua conta Uber está suspensa por {remaining.Hours}h {remaining.Minutes}m";
            }
            else
            {
                return $"Sua conta Uber está suspensa por {remaining.Minutes}m";
            }
        }

        private DateTime CalculateBanDuration(BanType type)
        {
            switch (type)
            {
                case BanType.Short:
                    return DateTime.UtcNow.AddMinutes(30);
                case BanType.Medium:
                    return DateTime.UtcNow.AddHours(2);
                case BanType.Long:
                    return DateTime.UtcNow.AddHours(24);
                default:
                    return DateTime.UtcNow.AddMinutes(30);
            }
        }
    }
}