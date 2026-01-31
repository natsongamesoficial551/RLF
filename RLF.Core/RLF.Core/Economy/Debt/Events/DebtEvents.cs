using RLF.Core.Economy.Transactions;

namespace RLF.Core.Economy.Events
{
    public class DebtStartedEvent
    {
        public decimal DebtAmount { get; }

        public DebtStartedEvent(decimal debtAmount)
        {
            DebtAmount = debtAmount;
        }
    }

    public class DebtClearedEvent
    {
        public DebtClearedEvent() { }
    }

    public class InterestAppliedEvent
    {
        public EconomyTransaction Transaction { get; }

        public InterestAppliedEvent(EconomyTransaction transaction)
        {
            Transaction = transaction;
        }
    }
}
