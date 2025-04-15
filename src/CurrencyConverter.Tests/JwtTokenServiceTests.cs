using CurrencyConverter.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace CurrencyConverter.Tests
{
    public class JwtTokenServiceTests
    {
        private readonly JwtSettings _jwtSettings;
        private readonly JwtTokenService _jwtTokenService;

        public JwtTokenServiceTests()
        {
            _jwtSettings = new JwtSettings
            {
                Secret = "ThisIsAVerySecureSecretKeyForTesting12345",
                ExpiryMinutes = 60,
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };

            var mockOptions = new Mock<IOptions<JwtSettings>>();
            mockOptions.Setup(x => x.Value).Returns(_jwtSettings);

            _jwtTokenService = new JwtTokenService(mockOptions.Object);
        }

        [Fact]
        public void GenerateToken_ReturnsValidToken()
        {
            // Arrange
            var userId = "testuser";
            var clientId = "test-client-id";
            var roles = new List<string> { "User", "Admin" };

            // Act
            var token = _jwtTokenService.GenerateToken(userId, clientId, roles);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateToken_ContainsExpectedClaims()
        {
            // Arrange
            var userId = "testuser";
            var clientId = "test-client-id";
            var roles = new List<string> { "User", "Admin" };

            // Act
            var token = _jwtTokenService.GenerateToken(userId, clientId, roles);

            // Decode the token to verify claims
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            // Assert
            Assert.Equal(userId, principal.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal(clientId, principal.FindFirstValue("ClientId"));
            Assert.Equal("User", principal.FindFirstValue(ClaimTypes.Role));
            Assert.Equal("Admin", principal.FindAll(ClaimTypes.Role).ElementAt(1).Value);
        }

        [Fact]
        public void GenerateToken_HasCorrectExpiry()
        {
            // Arrange
            var userId = "testuser";
            var clientId = "test-client-id";
            var roles = new List<string> { "User" };

            // Act
            var token = _jwtTokenService.GenerateToken(userId, clientId, roles);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Assert
            var expiry = jwtToken.ValidTo;
            var expectedExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

            // Allow a small tolerance for test execution time
            Assert.True(Math.Abs((expectedExpiry - expiry).TotalSeconds) < 5);
        }
    }
}
