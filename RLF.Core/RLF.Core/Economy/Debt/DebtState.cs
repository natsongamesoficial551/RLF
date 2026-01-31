using System;

namespace RLF.Core.Economy.Debt
{
    public class DebtState
    {
        public bool InDebt { get; internal set; }
        public decimal CurrentDebt { get; internal set; }
        public DateTime? DebtStartedAt { get; internal set; }
        public DateTime? LastInterestAppliedAt { get; internal set; }
    }
}
