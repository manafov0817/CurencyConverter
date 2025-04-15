using CurrencyConverter.Api.Models.Auth;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Tests.Models.Auth
{
    public class LoginResponseTests
    {
        [Fact]
        public void LoginResponse_Properties_WorkAsExpected()
        {
            // Arrange
            var loginResponse = new LoginResponse
            {
                Username = "testuser",
                Token = "jwt-token-123",
                Roles = new List<string> { "Admin", "User" }
            };

            // Assert
            Assert.Equal("testuser", loginResponse.Username);
            Assert.Equal("jwt-token-123", loginResponse.Token);
            Assert.Collection(loginResponse.Roles,
                role => Assert.Equal("Admin", role),
                role => Assert.Equal("User", role));
        }

        [Fact]
        public void LoginResponse_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var loginResponse = new LoginResponse();

            // Assert
            Assert.Equal(string.Empty, loginResponse.Username);
            Assert.Equal(string.Empty, loginResponse.Token);
            Assert.NotNull(loginResponse.Roles);
            Assert.Empty(loginResponse.Roles);
        }

        [Fact]
        public void LoginResponse_Serialization_WorksCorrectly()
        {
            // Arrange
            var loginResponse = new LoginResponse
            {
                Username = "testuser",
                Token = "jwt-token-123",
                Roles = new List<string> { "Admin", "User" }
            };

            // Act
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(loginResponse, options);
            var deserialized = JsonSerializer.Deserialize<LoginResponse>(json, options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(loginResponse.Username, deserialized.Username);
            Assert.Equal(loginResponse.Token, deserialized.Token);
            Assert.Equal(loginResponse.Roles.Count, deserialized.Roles.Count);
            Assert.Contains("Admin", deserialized.Roles);
            Assert.Contains("User", deserialized.Roles);
        }

        [Fact]
        public void LoginResponse_EmptyRoles_SerializesCorrectly()
        {
            // Arrange
            var loginResponse = new LoginResponse
            {
                Username = "testuser",
                Token = "jwt-token-123",
                Roles = new List<string>()
            };

            // Act
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(loginResponse, options);
            var deserialized = JsonSerializer.Deserialize<LoginResponse>(json, options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Empty(deserialized.Roles);
        }
    }
}
