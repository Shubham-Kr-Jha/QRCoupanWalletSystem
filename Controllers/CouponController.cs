using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using QRCoupanWalletSystem.Services;

namespace QRCoupanWalletSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem([FromBody] RedeemDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            var ok = await _couponService.RedeemCoupon(userId, dto.Code, dto.IdempotencyKey);
            if (!ok) return BadRequest(new { success = false });
            return Ok(new { success = true });
        }

        public record RedeemDto(string Code, string IdempotencyKey);
    }
}