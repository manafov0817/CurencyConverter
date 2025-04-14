using System.Collections.Generic;

namespace CurrencyConverter.Api.Models
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Username { get; set; }
        public string Token { get; set; }
        public List<string> Roles { get; set; }
    }
}
