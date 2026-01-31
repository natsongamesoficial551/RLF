using RLF.Core.Economy.Transactions;

namespace RLF.Core.Economy.Events
{
    public class ExpenseAppliedEvent
    {
        public EconomyTransaction Transaction { get; }

        public ExpenseAppliedEvent(EconomyTransaction transaction)
        {
            Transaction = transaction;
        }
    }
}
