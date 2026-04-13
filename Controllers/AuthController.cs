using Microsoft.AspNetCore.Mvc;
using QRCoupanWalletSystem.Services;

namespace QRCoupanWalletSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var user = await _auth.Register(dto.Email, dto.Password);
                return Ok(new { user.Id, user.Email });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _auth.Login(dto.Email, dto.Password);
            if (token == null) return Unauthorized();
            return Ok(new { token });
        }
    }

    public record RegisterDto(string Email, string Password);
    public record LoginDto(string Email, string Password);
}