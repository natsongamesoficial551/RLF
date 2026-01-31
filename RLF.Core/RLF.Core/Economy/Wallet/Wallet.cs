using RLF.Core.Economy.Events;
using RLF.Core.Economy.Transactions;
using System;

namespace RLF.Core.Economy.Wallet
{
    public class Wallet
    {
        public decimal Balance { get; private set; }

        private readonly WalletSettings _settings;

        // Eventos (podem ser conectados ao EventManager do Core)
        public event Action<MoneyChangedEvent> OnMoneyChanged;
        public event Action<BalanceLimitReachedEvent> OnBalanceLimitReached;

        public Wallet(decimal initialBalance, WalletSettings settings)
        {
            Balance = initialBalance;
            _settings = settings ?? new WalletSettings();
        }

        public bool ApplyTransaction(EconomyTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            decimal oldBalance = Balance;
            decimal newBalance = Balance + transaction.Amount;

            // Limite positivo
            if (newBalance > _settings.MaxBalanceLimit)
            {
                Balance = _settings.MaxBalanceLimit;
                OnBalanceLimitReached?.Invoke(new BalanceLimitReachedEvent(Balance));
                return false;
            }

            // Limite negativo
            if (!_settings.AllowNegativeBalance && newBalance < 0)
            {
                OnBalanceLimitReached?.Invoke(new BalanceLimitReachedEvent(Balance));
                return false;
            }

            if (newBalance < _settings.MinBalanceLimit)
            {
                Balance = _settings.MinBalanceLimit;
                OnBalanceLimitReached?.Invoke(new BalanceLimitReachedEvent(Balance));
                return false;
            }

            Balance = newBalance;

            OnMoneyChanged?.Invoke(
                new MoneyChangedEvent(oldBalance, Balance, transaction)
            );

            return true;
        }
    }
}
