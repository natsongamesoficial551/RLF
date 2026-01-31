using RLF.Core.Economy.Transactions;
using System;

namespace RLF.Core.Economy.Ledger
{
    public sealed class LedgerEntry
    {
        public Guid Id { get; }
        public DateTime Timestamp { get; }

        public EconomyTransaction Transaction { get; }
        public decimal BalanceAfter { get; }

        public LedgerEntry(EconomyTransaction transaction, decimal balanceAfter)
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;

            Transaction = transaction;
            BalanceAfter = balanceAfter;
        }
    }
}
