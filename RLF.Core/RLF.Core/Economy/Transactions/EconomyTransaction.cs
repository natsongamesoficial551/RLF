using System;

namespace RLF.Core.Economy.Transactions
{
    public sealed class EconomyTransaction
    {
        public Guid Id { get; }
        public DateTime Timestamp { get; }

        public decimal Amount { get; }
        public TransactionType Type { get; }
        public TransactionLegality Legality { get; }
        public TransactionOrigin Origin { get; }

        public string Description { get; }

        public EconomyTransaction(
            decimal amount,
            TransactionType type,
            TransactionLegality legality,
            TransactionOrigin origin,
            string description = "")
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;

            Amount = amount;
            Type = type;
            Legality = legality;
            Origin = origin;
            Description = description;
        }
    }
}
