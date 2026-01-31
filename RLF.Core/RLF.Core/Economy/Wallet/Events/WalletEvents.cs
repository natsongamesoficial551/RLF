using RLF.Core.Economy.Transactions;

namespace RLF.Core.Economy.Events
{
    public class MoneyChangedEvent
    {
        public decimal OldBalance { get; }
        public decimal NewBalance { get; }
        public EconomyTransaction Transaction { get; }

        public MoneyChangedEvent(decimal oldBalance, decimal newBalance, EconomyTransaction transaction)
        {
            OldBalance = oldBalance;
            NewBalance = newBalance;
            Transaction = transaction;
        }
    }

    public class BalanceLimitReachedEvent
    {
        public decimal Balance { get; }

        public BalanceLimitReachedEvent(decimal balance)
        {
            Balance = balance;
        }
    }
}
