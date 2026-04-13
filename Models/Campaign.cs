using System.ComponentModel.DataAnnotations;

namespace QRCoupanWalletSystem.Models
{
    public class Campaign
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public List<Coupon> Coupons { get; set; } = new();
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}