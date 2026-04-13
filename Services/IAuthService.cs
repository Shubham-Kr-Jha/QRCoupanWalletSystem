using QRCoupanWalletSystem.Models;

namespace QRCoupanWalletSystem.Services
{
    public interface IAuthService
    {
        Task<User> Register(string email, string password);
        Task<User> RegisterAdmin(string email, string password);
        Task<string?> Login(string email, string password);
    }
}