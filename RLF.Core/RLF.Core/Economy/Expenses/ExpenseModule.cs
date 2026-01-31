using RLF.Core.Economy.Events;
using RLF.Core.Economy.Transactions;
using System;
using WalletModel = RLF.Core.Economy.Wallet.Wallet;

namespace RLF.Core.Economy.Expenses
{
    public class ExpenseModule
    {
        private readonly WalletModel _wallet;
        private readonly ExpenseSettings _settings;

        public event Action<ExpenseAppliedEvent> OnExpenseApplied;

        public ExpenseModule(WalletModel wallet, ExpenseSettings settings)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _settings = settings ?? new ExpenseSettings();
        }

        /// <summary>
        /// Aplica gastos diários básicos (ativos desde já).
        /// </summary>
        public void ApplyDailyExpenses()
        {
            ApplyExpense(
                _settings.DailyLivingCost,
                ExpenseType.LivingCost,
                "Custo de vida diário"
            );

            ApplyExpense(
                _settings.DailyTransportCost,
                ExpenseType.Transport,
                "Transporte diário"
            );
        }

        /// <summary>
        /// Aplica taxas semanais básicas.
        /// </summary>
        public void ApplyWeeklyExpenses()
        {
            ApplyExpense(
                _settings.WeeklyBasicTax,
                ExpenseType.TaxBasic,
                "Taxa básica semanal"
            );
        }

        /// <summary>
        /// Futuro: aluguel e contas (somente se HasHouse = true).
        /// </summary>
        public void ApplyHousingExpenses(decimal rent, decimal utilities)
        {
            if (!_settings.HasHouse)
                return;

            ApplyExpense(rent, ExpenseType.HousingRent, "Aluguel");
            ApplyExpense(utilities, ExpenseType.Utilities, "Contas da casa");
        }

        private void ApplyExpense(decimal amount, ExpenseType type, string description)
        {
            if (amount <= 0)
                return;

            var transaction = new EconomyTransaction(
                amount: -amount, // gasto sempre negativo
                type: TransactionType.Expense,
                legality: TransactionLegality.Legal,
                origin: TransactionOrigin.LivingCost,
                description: description
            );

            bool applied = _wallet.ApplyTransaction(transaction);

            if (applied)
            {
                OnExpenseApplied?.Invoke(
                    new ExpenseAppliedEvent(transaction)
                );
            }
        }
    }
}
