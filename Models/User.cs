using System.ComponentModel.DataAnnotations;

namespace QRCoupanWalletSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string PasswordHash { get; set; } = null!;

        public Wallet? Wallet { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Role { get; set; } = "User";
    }
}