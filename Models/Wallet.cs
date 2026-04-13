using System.ComponentModel.DataAnnotations;

namespace QRCoupanWalletSystem.Models
{
    public class Wallet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        public decimal Balance { get; set; }

        public List<Transaction> Transactions { get; set; } = new();
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}