namespace QRCoupanWalletSystem.Services
{
    public interface ICouponService
    {
        Task<bool> RedeemCoupon(int userId, string couponCode, string idempotencyKey);
        Task ReconcileAsync();
    }
}