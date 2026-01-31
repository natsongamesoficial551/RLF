using System;
using RLF.Core.Economy.Wallet;
using RLF.Core.Economy.Ledger;
using RLF.Core.Economy.Expenses;
using RLF.Core.Economy.Debt;
using RLF.Core.Economy.Transactions;
using RLF.Core.Economy.Events;

namespace RLF.Core.Economy
{
    public class EconomySystem
    {
        public Wallet.Wallet Wallet { get; }
        public TransactionLedger Ledger { get; }

        public ExpenseModule Expenses { get; }
        public DebtModule Debt { get; }

        public EconomySystem(
            decimal initialBalance,
            WalletSettings walletSettings,
            ExpenseSettings expenseSettings,
            DebtSettings debtSettings)
        {
            // 1️⃣ Base
            Wallet = new Wallet.Wallet(initialBalance, walletSettings);
            Ledger = new TransactionLedger();

            // 2️⃣ Módulos
            Expenses = new ExpenseModule(Wallet, expenseSettings);
            Debt = new DebtModule(Wallet, debtSettings);

            // 3️⃣ Conexões
            Wallet.OnMoneyChanged += HandleMoneyChanged;
        }

        /// <summary>
        /// Aplica uma transação manualmente (salários, roubos, bônus, multas).
        /// </summary>
        public bool ApplyTransaction(EconomyTransaction transaction)
        {
            return Wallet.ApplyTransaction(transaction);
        }

        /// <summary>
        /// Deve ser chamado quando um "dia lógico" passa.
        /// </summary>
        public void OnNewDay()
        {
            Expenses.ApplyDailyExpenses();
            Debt.ApplyDailyInterest();
        }

        /// <summary>
        /// Deve ser chamado quando uma "semana lógica" passa.
        /// </summary>
        public void OnNewWeek()
        {
            Expenses.ApplyWeeklyExpenses();
        }

        private void HandleMoneyChanged(MoneyChangedEvent evt)
        {
            // 1️⃣ Registra no Ledger
            Ledger.Record(
                new LedgerEntry(
                    evt.Transaction,
                    evt.NewBalance
                )
            );

            // 2️⃣ Avalia dívida
            Debt.EvaluateDebt(evt.NewBalance);
        }
    }
}
