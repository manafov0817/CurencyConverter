using CurrencyConverter.Api.Controllers;
using CurrencyConverter.Api.Models.Auth;
using CurrencyConverter.Core.Configuration;
using CurrencyConverter.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace CurrencyConverter.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IOptions<UserCredentialsOptions>> _mockUserCredentialsOptions;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockUserCredentialsOptions = new Mock<IOptions<UserCredentialsOptions>>();

            // Setup mock user credentials
            _mockUserCredentialsOptions.Setup(x => x.Value).Returns(new UserCredentialsOptions
            {
                Users = new List<UserCredential>
                {
                    new UserCredential
                    {
                        Username = "admin",
                        Password = "admin",
                        Roles = new List<string> { "Admin" }
                    },
                    new UserCredential
                    {
                        Username = "user",
                        Password = "user",
                        Roles = new List<string> { "User" }
                    }
                }
            });

            _controller = new AuthController(_mockJwtTokenService.Object, _mockLogger.Object, _mockUserCredentialsOptions.Object);

            // Setup HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public void Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var request = new LoginRequest { Username = "admin", Password = "admin" };
            var token = "test-jwt-token";
            _mockJwtTokenService.Setup(s => s.GenerateToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>()
            )).Returns(token);

            // Act
            var result = _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal(request.Username, response.Username);
            Assert.Equal(token, response.Token);
            Assert.Contains("Admin", response.Roles);
        }

        [Fact]
        public void Login_WithValidUserCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var request = new LoginRequest { Username = "user", Password = "user" };
            var token = "test-jwt-token";
            _mockJwtTokenService.Setup(s => s.GenerateToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>()
            )).Returns(token);

            // Act
            var result = _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal(request.Username, response.Username);
            Assert.Equal(token, response.Token);
            Assert.Contains("User", response.Roles);
        }

        [Fact]
        public void Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest { Username = "admin", Password = "wrongpassword" };

            // Act
            var result = _controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void Login_WithNonExistentUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest { Username = "nonexistent", Password = "password" };

            // Act
            var result = _controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void Login_WithEmptyUsername_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = "", Password = "password" };

            // Act
            var result = _controller.Login(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Login_WithEmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = "admin", Password = "" };

            // Act
            var result = _controller.Login(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Login_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            LoginRequest request = new() { Username = null, Password = null };

            // Act
            var result = _controller.Login(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
