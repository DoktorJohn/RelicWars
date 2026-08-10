using Domain.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Application.Services.Authentication
{
    public sealed class LegacyCompatiblePasswordHasher : IPasswordHasher<PlayerProfile>
    {
        private readonly PasswordHasher<PlayerProfile> _identityHasher;

        public LegacyCompatiblePasswordHasher(IOptions<PasswordHasherOptions> options)
        {
            _identityHasher = new PasswordHasher<PlayerProfile>(options);
        }

        public string HashPassword(PlayerProfile user, string password)
        {
            return _identityHasher.HashPassword(user, password);
        }

        public PasswordVerificationResult VerifyHashedPassword(
            PlayerProfile user,
            string hashedPassword,
            string providedPassword)
        {
            if (IsLegacyBcryptHash(hashedPassword))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                        ? PasswordVerificationResult.SuccessRehashNeeded
                        : PasswordVerificationResult.Failed;
                }
                catch (Exception)
                {
                    return PasswordVerificationResult.Failed;
                }
            }

            return _identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }

        private static bool IsLegacyBcryptHash(string hashedPassword)
        {
            return hashedPassword.StartsWith("$2a$", StringComparison.Ordinal)
                || hashedPassword.StartsWith("$2b$", StringComparison.Ordinal)
                || hashedPassword.StartsWith("$2y$", StringComparison.Ordinal);
        }
    }
}
