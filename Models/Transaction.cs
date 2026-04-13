using System.ComponentModel.DataAnnotations;

namespace QRCoupanWalletSystem.Models
{
    public enum TransactionType { Credit, Debit }

    public class Transaction
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public Wallet? Wallet { get; set; }

        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }

        // Idempotency token or external id to avoid duplicates
        public string? ExternalId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Note { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}