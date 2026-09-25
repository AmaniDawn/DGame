using NUnit.Framework;
using Unity.Pipeline.Security;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for security token generation and comparison.
    /// </summary>
    public class SecurityTokenTests
    {
        [SetUp]
        public void SetUp() => SecurityTokenManager.ClearCache();

        [TearDown]
        public void TearDown() => SecurityTokenManager.ClearCache();

        [Test]
        public void GetOrCreateToken_FirstCall_GeneratesToken()
        {
            var token = SecurityTokenManager.GetOrCreateToken();

            Assert.IsNotNull(token, "Token should not be null");
            Assert.IsNotEmpty(token, "Token should not be empty");
            Assert.GreaterOrEqual(token.Length, 32, "Token should be at least 32 characters (256-bit base64)");
        }

        [Test]
        public void GetOrCreateToken_SecondCall_ReturnsSameToken()
        {
            var firstToken = SecurityTokenManager.GetOrCreateToken();
            var secondToken = SecurityTokenManager.GetOrCreateToken();

            Assert.AreEqual(firstToken, secondToken, "Should return the same token on subsequent calls");
        }

        [Test]
        public void ClearCache_AfterTokenGeneration_GeneratesDifferentToken()
        {
            var originalToken = SecurityTokenManager.GetOrCreateToken();

            SecurityTokenManager.ClearCache();
            var newToken = SecurityTokenManager.GetOrCreateToken();

            Assert.IsNotNull(newToken);
            Assert.AreNotEqual(originalToken, newToken, "A fresh token should be generated after the cache is cleared");
        }

        [Test]
        public void ConstantTimeEquals_IdenticalTokens_ReturnsTrue()
        {
            var token = SecurityTokenManager.GetOrCreateToken();
            Assert.IsTrue(SecurityTokenManager.ConstantTimeEquals(token, token));
        }

        [Test]
        public void ConstantTimeEquals_DifferentTokens_ReturnsFalse()
        {
            var token = SecurityTokenManager.GetOrCreateToken();
            Assert.IsFalse(SecurityTokenManager.ConstantTimeEquals(token, "not-the-token"));
        }

        [TestCase(null, TestName = "ConstantTimeEquals_NullToken_ReturnsFalse")]
        [TestCase("", TestName = "ConstantTimeEquals_EmptyToken_ReturnsFalse")]
        public void ConstantTimeEquals_NullOrEmpty_ReturnsFalse(string token)
        {
            var expected = SecurityTokenManager.GetOrCreateToken();
            Assert.IsFalse(SecurityTokenManager.ConstantTimeEquals(token, expected));
        }
    }
}
