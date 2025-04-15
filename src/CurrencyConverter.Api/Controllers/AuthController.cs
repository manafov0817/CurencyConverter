using CurrencyConverter.Api.Models.Auth;
using CurrencyConverter.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Linq;
using CurrencyConverter.Core.Configuration;

namespace CurrencyConverter.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;
        private readonly UserCredentialsOptions _userCredentialsOptions;

        public AuthController(IJwtTokenService jwtTokenService,
                              ILogger<AuthController> logger,
                              IOptions<UserCredentialsOptions> userCredentialsOptions)
        {
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userCredentialsOptions = userCredentialsOptions?.Value ?? throw new ArgumentNullException(nameof(userCredentialsOptions));
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

            var userInfo = _userCredentialsOptions.Users.FirstOrDefault(u => 
                u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));

            if (userInfo == null || userInfo.Password != request.Password)
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
                userInfo.Roles
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
                Roles = userInfo.Roles,
            });
        }
    }
}
