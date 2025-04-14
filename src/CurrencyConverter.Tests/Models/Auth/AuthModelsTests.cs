using CurrencyConverter.Api.Models.Auth;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Tests.Models.Auth
{
    public class AuthModelsTests
    {
        [Fact]
        public void LoginRequest_Properties_WorkAsExpected()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Password = "password123"
            };

            // Assert
            Assert.Equal("testuser", loginRequest.Username);
            Assert.Equal("password123", loginRequest.Password);
        }

        [Fact]
        public void LoginRequest_DefaultValues_AreEmpty()
        {
            // Arrange
            var loginRequest = new LoginRequest();

            // Assert
            Assert.Equal(string.Empty, loginRequest.Username);
            Assert.Equal(string.Empty, loginRequest.Password);
        }

        [Fact]
        public void LoginRequest_Serialization_WorksCorrectly()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Password = "password123"
            };

            // Act
            var json = JsonSerializer.Serialize(loginRequest);
            var deserialized = JsonSerializer.Deserialize<LoginRequest>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(loginRequest.Username, deserialized.Username);
            Assert.Equal(loginRequest.Password, deserialized.Password);
        }

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
            // Arrange
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
            var json = JsonSerializer.Serialize(loginResponse);
            var deserialized = JsonSerializer.Deserialize<LoginResponse>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(loginResponse.Username, deserialized.Username);
            Assert.Equal(loginResponse.Token, deserialized.Token);
            Assert.Equal(loginResponse.Roles.Count, deserialized.Roles.Count);
            Assert.Contains("Admin", deserialized.Roles);
            Assert.Contains("User", deserialized.Roles);
        }
    }
}
