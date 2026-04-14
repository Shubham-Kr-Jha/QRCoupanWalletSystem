using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QRCoupanWalletSystem.Data;
using QRCoupanWalletSystem.Models;
using QRCoupanWalletSystem.Services;
using QRCoder;

namespace QRCoupanWalletSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICouponService _couponService;
        private readonly QRCoupanWalletSystem.Services.IAuthService _authService;
        private readonly IConfiguration? _config;

        public AdminController(AppDbContext db, ICouponService couponService, QRCoupanWalletSystem.Services.IAuthService authService, IConfiguration? config = null)
        {
            _db = db;
            _couponService = couponService;
            _authService = authService;
            _config = config;
        }

        [HttpPost("campaigns/{campaignId}/soft-delete")]
        public async Task<IActionResult> SoftDeleteCampaign(int campaignId)
        {
            var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
            if (campaign == null) return NotFound();
            campaign.IsDeleted = true;
            campaign.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("coupons/{couponId}/soft-delete")]
        public async Task<IActionResult> SoftDeleteCoupon(int couponId)
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == couponId);
            if (coupon == null) return NotFound();
            coupon.IsDeleted = true;
            coupon.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("campaigns")]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignDto dto)
        {
            var campaign = new Campaign { Name = dto.Name, StartsAt = dto.StartsAt, EndsAt = dto.EndsAt };
            _db.Campaigns.Add(campaign);
            await _db.SaveChangesAsync();
            return Ok(campaign);
        }

        [HttpPost("campaigns/{campaignId}/generateCoupons")]
        public async Task<IActionResult> GenerateCoupons(int campaignId, [FromBody] GenerateDto dto)
        {
            var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
            if (campaign == null) return NotFound();

            var coupons = new List<Coupon>();
            var result = new List<object>();
            for (int i = 0; i < dto.Count; i++)
            {
                var code = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
                var coupon = new Coupon { Code = code, CampaignId = campaignId, Amount = dto.Amount };
                coupons.Add(coupon);
            }
            _db.Coupons.AddRange(coupons);
            await _db.SaveChangesAsync();

            // Generate QR codes (PNG base64) for each coupon using PngByteQRCode
            var generator = new QRCoder.QRCodeGenerator();
            foreach (var c in coupons)
            {
                var payload = c.Code; // simple payload; can include signed data if required
                var data = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.M);
                var png = new QRCoder.PngByteQRCode(data).GetGraphic(20);
                var base64 = Convert.ToBase64String(png);
                var dataUrl = $"data:image/png;base64,{base64}";
                result.Add(new { code = c.Code, qrcode = dataUrl });
            }

            return Ok(result);
        }

        [HttpPost("reconcile")]
        public async Task<IActionResult> Reconcile()
        {
            await _couponService.ReconcileAsync();
            return Ok(new { reconciled = true });
        }

        public record CreateCampaignDto(string Name, DateTime StartsAt, DateTime EndsAt);
        public record GenerateDto(int Count, decimal Amount);
    }
}