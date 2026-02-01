using System;

namespace Accounting.Plugin.Domain
{
    public class LedgerEntry
    {
        public Guid Id { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string EntryType { get; set; } = "Debit"; // Debit or Credit
    }
}
