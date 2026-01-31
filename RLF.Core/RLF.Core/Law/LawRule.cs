using RLF.Core.Identity.Enums;

namespace RLF.Core.Law
{
    /// <summary>
    /// Regra legal para uma violação específica.
    /// </summary>
    public sealed class LawRule
    {
        public ViolationType Violation { get; }
        public LawActionType Action { get; }
        public int FineAmount { get; }
        public bool RequiresArrest { get; }

        public LawRule(
            ViolationType violation,
            LawActionType action,
            int fineAmount = 0,
            bool requiresArrest = false)
        {
            Violation = violation;
            Action = action;
            FineAmount = fineAmount;
            RequiresArrest = requiresArrest;
        }
    }
}
