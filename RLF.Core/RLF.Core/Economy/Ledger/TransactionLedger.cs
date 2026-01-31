using RLF.Core.Economy.Transactions;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Economy.Ledger
{
    public class TransactionLedger
    {
        private readonly List<LedgerEntry> _entries = new List<LedgerEntry>();

        public IReadOnlyList<LedgerEntry> Entries
        {
            get { return _entries; }
        }

        public void Record(LedgerEntry entry)
        {
            if (entry == null)
                return;

            _entries.Add(entry);
        }

        public IReadOnlyList<LedgerEntry> GetRecent(int count)
        {
            return _entries
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();
        }

        public IReadOnlyList<LedgerEntry> GetByLegality(TransactionLegality legality)
        {
            return _entries
                .Where(e => e.Transaction.Legality == legality)
                .ToList();
        }
    }
}
