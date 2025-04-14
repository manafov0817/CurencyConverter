using CurrencyConverter.Api.Models.Auth;
using CurrencyConverter.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CurrencyConverter.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;

        // In a real application, this would be replaced with a proper user repository
        private static readonly Dictionary<string, (string password, List<string> roles)> _users =
            new Dictionary<string, (string, List<string>)>
            {
                { "admin", ("admin123", new List<string> { "Admin" }) },
                { "user", ("user123", new List<string> { "User" }) },
            };

        public AuthController(IJwtTokenService jwtTokenService,
                              ILogger<AuthController> logger)
        {
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            if (!_users.TryGetValue(request.Username, out var userInfo)
                || userInfo.password != request.Password)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Failed login attempt for user {Username} from {ClientIp} | Time: {ElapsedMs}ms",
                    request.Username,
                    clientIp,
                    stopwatch.ElapsedMilliseconds
                );

                return Unauthorized("Invalid username or password");
            }

            var clientId = Guid.NewGuid().ToString();
            var token = _jwtTokenService.GenerateToken(
                request.Username,
                clientId,
                userInfo.roles
            );

            stopwatch.Stop();
            _logger.LogInformation(
                "Successful login for user {Username} from {ClientIp} | ClientId: {ClientId} | Time: {ElapsedMs}ms",
                request.Username,
                clientIp,
                clientId,
                stopwatch.ElapsedMilliseconds
            );

            return Ok(new LoginResponse
            {
                Username = request.Username,
                Token = token,
                Roles = userInfo.roles,
            });
        }
    }
}
