using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CurrencyConverter.Api.Models.Auth
{
    public class LoginResponse
    {
        public string Username { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }
}
