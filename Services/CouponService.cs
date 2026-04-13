using Microsoft.EntityFrameworkCore;
using QRCoupanWalletSystem.Data;
using QRCoupanWalletSystem.Models;

namespace QRCoupanWalletSystem.Services
{
    public class CouponService : ICouponService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CouponService> _logger;

        public CouponService(AppDbContext db, ILogger<CouponService> logger)
        {
            _db = db;
            _logger = logger;
        }
        public async Task<bool> RedeemCoupon(int userId, string couponCode, string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(couponCode)) return false;
            if (string.IsNullOrWhiteSpace(idempotencyKey)) return false;

            using var txn = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var coupon = await _db.Coupons.Where(c => c.Code == couponCode).FirstOrDefaultAsync();
                if (coupon == null) return false;

                var now = DateTime.UtcNow;
                if (coupon.Campaign != null)
                {
                }

                if (coupon.Redeemed)
                {
                    if (!string.IsNullOrEmpty(coupon.RedemptionIdempotencyKey) && coupon.RedemptionIdempotencyKey == idempotencyKey)
                    {
                        await txn.CommitAsync();
                        return true; 
                    }
                    return false;
                }
                var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == coupon.CampaignId);
                if (campaign != null)
                {
                    if (campaign.StartsAt > now || campaign.EndsAt < now) return false;
                }

                var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null) return false;

                var existingTx = await _db.Transactions.FirstOrDefaultAsync(t => t.ExternalId == idempotencyKey && t.WalletId == wallet.Id);
                if (existingTx != null)
                {
                    await txn.CommitAsync();
                    return true; 
                }

                var tx = new Transaction
                {
                    WalletId = wallet.Id,
                    Amount = coupon.Amount,
                    Type = TransactionType.Credit,
                    ExternalId = idempotencyKey,
                    Note = $"Redeem coupon {coupon.Code}"
                };

                // Update wallet balance
                wallet.Balance += coupon.Amount;
                _db.Transactions.Add(tx);

                // Mark coupon redeemed
                coupon.Redeemed = true;
                coupon.RedeemedAt = DateTime.UtcNow;
                coupon.RedeemedByUserId = userId;
                coupon.RedemptionIdempotencyKey = idempotencyKey;

                await _db.SaveChangesAsync();
                await txn.CommitAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency issue while redeeming coupon {CouponCode}", couponCode);
                await txn.RollbackAsync();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redeeming coupon {CouponCode}", couponCode);
                await txn.RollbackAsync();
                return false;
            }
        }
        public async Task ReconcileAsync()
        {

            var txs = await _db.Transactions
                .Where(t => t.Note != null && t.Note.StartsWith("Redeem coupon "))
                .ToListAsync();

            foreach (var tx in txs)
            {
                try
                {
                    var code = tx.Note!.Substring("Redeem coupon ".Length);
                    var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
                    if (coupon == null)
                    {
                        _logger.LogWarning("Transaction {TxId} references missing coupon {Code}", tx.Id, code);
                        continue;
                    }

                    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == tx.WalletId);
                    if (wallet == null)
                    {
                        _logger.LogWarning("Transaction {TxId} references missing wallet {WalletId}", tx.Id, tx.WalletId);
                        continue;
                    }

                    if (!coupon.Redeemed)
                    {
                        coupon.Redeemed = true;
                        coupon.RedeemedAt = tx.CreatedAt;
                        coupon.RedeemedByUserId = wallet.UserId;
                        coupon.RedemptionIdempotencyKey = tx.ExternalId;
                        _db.Coupons.Update(coupon);
                        _logger.LogInformation("Marked coupon {Code} redeemed based on transaction {TxId}", code, tx.Id);
                    }

                    var computed = await _db.Transactions.Where(t => t.WalletId == wallet.Id).SumAsync(t => t.Type == TransactionType.Credit ? t.Amount : -t.Amount);
                    if (computed != wallet.Balance)
                    {
                        _logger.LogInformation("Wallet {WalletId} balance inconsistent (db:{DbBalance} computed:{Computed}). Fixing.", wallet.Id, wallet.Balance, computed);
                        wallet.Balance = computed;
                        _db.Wallets.Update(wallet);
                    }

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reconciling transaction {TxId}", tx.Id);
                }
            }
        }
    }
}