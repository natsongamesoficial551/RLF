using System;
using RLF.Core.Economy.Transactions;
using RLF.Core.Economy.Events;
using WalletModel = RLF.Core.Economy.Wallet.Wallet;

namespace RLF.Core.Economy.Debt
{
    public class DebtModule
    {
        private readonly WalletModel _wallet;
        private readonly DebtSettings _settings;

        public DebtState State { get; } = new DebtState();

        public event Action<DebtStartedEvent> OnDebtStarted;
        public event Action<DebtClearedEvent> OnDebtCleared;
        public event Action<InterestAppliedEvent> OnInterestApplied;

        public DebtModule(WalletModel wallet, DebtSettings settings)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _settings = settings ?? new DebtSettings();
        }

        /// <summary>
        /// Deve ser chamado após mudanças no saldo.
        /// </summary>
        public void EvaluateDebt(decimal currentBalance)
        {
            if (!_settings.Enabled)
                return;

            if (currentBalance <= _settings.DebtThreshold)
            {
                if (!State.InDebt)
                {
                    State.InDebt = true;
                    State.CurrentDebt = Math.Abs(currentBalance);
                    State.DebtStartedAt = DateTime.UtcNow;

                    OnDebtStarted?.Invoke(
                        new DebtStartedEvent(State.CurrentDebt)
                    );
                }
                else
                {
                    State.CurrentDebt = Math.Abs(currentBalance);
                }
            }
            else
            {
                if (State.InDebt)
                {
                    State.InDebt = false;
                    State.CurrentDebt = 0;
                    State.DebtStartedAt = null;
                    State.LastInterestAppliedAt = null;

                    OnDebtCleared?.Invoke(new DebtClearedEvent());
                }
            }
        }

        /// <summary>
        /// Aplica juros simples sobre a dívida atual.
        /// </summary>
        public void ApplyDailyInterest()
        {
            if (!_settings.Enabled || !State.InDebt)
                return;

            // Evita aplicar juros múltiplas vezes no mesmo dia
            if (State.LastInterestAppliedAt.HasValue &&
                State.LastInterestAppliedAt.Value.Date == DateTime.UtcNow.Date)
                return;

            decimal interestAmount = State.CurrentDebt * _settings.DailyInterestRate;

            if (interestAmount <= 0)
                return;

            var tx = new EconomyTransaction(
                amount: -interestAmount,
                type: TransactionType.DebtInterest,
                legality: TransactionLegality.Legal,
                origin: TransactionOrigin.Interest,
                description: "Juros diários da dívida"
            );

            bool applied = _wallet.ApplyTransaction(tx);
            if (applied)
            {
                State.LastInterestAppliedAt = DateTime.UtcNow;

                OnInterestApplied?.Invoke(
                    new InterestAppliedEvent(tx)
                );
            }
        }
    }
}
