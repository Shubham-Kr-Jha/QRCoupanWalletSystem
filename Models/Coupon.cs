using System.ComponentModel.DataAnnotations;

namespace QRCoupanWalletSystem.Models
{
    public class Coupon
    {
        public int Id { get; set; }
        [Required]
        public string Code { get; set; } = null!;

        public int CampaignId { get; set; }
        public Campaign? Campaign { get; set; }

        public decimal Amount { get; set; }
        public bool Redeemed { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public int? RedeemedByUserId { get; set; }

        // Idempotency token for redemption (to guard against duplicate requests)
        public string? RedemptionIdempotencyKey { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}