using System.Security.Cryptography;

namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Generates short, URL-safe invite tokens for the FamilyLink feature (US-Family).
    /// Tokens are cryptographically random Base64Url strings (8 bytes → 11 chars).
    /// </summary>
    public static class InviteTokenGenerator
    {
        /// <summary>
        /// Creates a unique, URL-safe token suitable for a FamilyLink invitation.
        /// Default length is 8 random bytes, producing an 11-character Base64Url string.
        /// </summary>
        public static string Generate(int byteLength = 8)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes)
                          .Replace('+', '-')
                          .Replace('/', '_')
                          .TrimEnd('=');
        }
    }
}
