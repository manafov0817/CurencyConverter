using System.Collections.Generic;

namespace CurrencyConverter.Core.Configuration
{
    public class UserCredentialsOptions
    {
        public const string UserCredentials = "UserCredentials";
        
        public List<UserCredential> Users { get; set; } = new List<UserCredential>();
    }

    public class UserCredential
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }
}
