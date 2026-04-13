using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QRCoupanWalletSystem.Data;
using QRCoupanWalletSystem.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QRCoupanWalletSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<User> Register(string email, string password)
        {
            if (await _db.Users.AnyAsync(u => u.Email == email))
                throw new ApplicationException("Email already registered");

            var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = "User" };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var wallet = new Wallet { UserId = user.Id, Balance = 0m };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();

            return user;
        }
        public async Task<string?> Login(string email, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            var jwtKey = _config["Jwt:Key"] ?? "ReplaceThisWithASecretKeyForDevOnly!";
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                }),
                Expires = DateTime.UtcNow.AddHours(6),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}