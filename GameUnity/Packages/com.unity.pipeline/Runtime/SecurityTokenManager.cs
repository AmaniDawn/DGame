using System;
using System.Security.Cryptography;

namespace Unity.Pipeline.Security
{
    /// <summary>
    /// Manages the security token used to authorize Pipeline server requests.
    /// The token is generated once per Unity session, held in memory, and published to the
    /// instance descriptor (port file) for CLI discovery. It is never written to a separate file.
    /// </summary>
    public static class SecurityTokenManager
    {
        private static string s_CachedToken;

        /// <summary>
        /// Get or create the security token for the current session.
        /// Generated once and cached in memory; regenerated after a domain reload.
        /// </summary>
        public static string GetOrCreateToken()
        {
            if (string.IsNullOrEmpty(s_CachedToken))
                s_CachedToken = GenerateSecureToken();

            return s_CachedToken;
        }

        /// <summary>
        /// Compare two tokens in length-independent constant time to avoid leaking the expected
        /// token through comparison timing.
        /// </summary>
        public static bool ConstantTimeEquals(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;

            var diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }

        /// <summary>
        /// Generate a cryptographically secure token.
        /// </summary>
        private static string GenerateSecureToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[32]; // 256 bits
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// Clear cached token (for testing or token rotation).
        /// </summary>
        public static void ClearCache()
        {
            s_CachedToken = null;
        }
    }
}
