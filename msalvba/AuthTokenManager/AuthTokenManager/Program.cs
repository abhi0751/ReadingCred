using System;
using System.Linq;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Identity.Client;
using Microsoft.Win32;

namespace AuthTokenManager
{
    internal class Program
    {
        private static readonly string[] DEFAULT_SCOPE = { "User.Read" };
        private const string DEFAULT_CLIENT_ID = "";
        private const string DEFAULT_TENANT_ID = "";
        private const int EXPIRY_THRESHOLD_MINUTES = 15;
        private static string RegistryPath = "";

        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: AuthTokenManager.exe <RegistryPath>");
                return 1;
            }

            RegistryPath = args[0];
            Console.WriteLine($"Getting token from registry: HKCU\\{RegistryPath}");

            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                //.WriteLine(token); // for VBA
                return 0;
            }

            Console.WriteLine("ERROR: Token acquisition failed");
            return 1;
        }

        private static string GetToken()
        {
            var token = ReadRegistryValue("AccessToken", string.Empty);
            if (!string.IsNullOrEmpty(token) && IsTokenValid(token))
            {
                Console.WriteLine("✅ Using valid token from registry.");
                return token;
            }

            Console.WriteLine("⚠️ Token missing, invalid, or expiring soon. Requesting new token...");
            token = AcquireToken();
            if (!string.IsNullOrEmpty(token))
            {
                StoreTokenInRegistry(token);
                return token;
            }

            return null;
        }

        private static bool IsTokenValid(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var exp = jwt.Payload.Exp;

                if (exp == null)
                {
                    Console.WriteLine("❌ No 'exp' claim found in token.");
                    return false;
                }

                var expTime = DateTimeOffset.FromUnixTimeSeconds((long)exp);
                var minutesRemaining = (expTime - DateTimeOffset.UtcNow).TotalMinutes;
                Console.WriteLine($"⏱ Token expires in {minutesRemaining:F0} minutes.");

                return minutesRemaining > EXPIRY_THRESHOLD_MINUTES;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validating token: {ex.Message}");
                return false;
            }
        }

        private static string AcquireToken()
        {
            var clientId = ReadRegistryValue("ClientId", DEFAULT_CLIENT_ID);
            var tenantId = ReadRegistryValue("TenantId", DEFAULT_TENANT_ID);
            var scopes = ReadRegistryValue("Scope", string.Join(",", DEFAULT_SCOPE))
                         .Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => s.Trim())
                         .ToArray();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(tenantId))
            {
                Console.WriteLine("❌ Client ID or Tenant ID missing in registry.");
                return null;
            }

            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithDefaultRedirectUri()
                .Build();

            AuthenticationResult result = null;

            try
            {
                var accounts = app.GetAccountsAsync().Result;
                var first = accounts.FirstOrDefault();
                if (first != null)
                    result = app.AcquireTokenSilent(scopes, first).ExecuteAsync().Result;
            }
            catch { }

            if (result == null)
                result = app.AcquireTokenInteractive(scopes).ExecuteAsync().Result;

            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                Console.WriteLine("✅ New token acquired.");
                return result.AccessToken;
            }

            Console.WriteLine("❌ Failed to acquire token.");
            return null;
        }

        private static void StoreTokenInRegistry(string token)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                key.SetValue("AccessToken", token, RegistryValueKind.String);
                key.SetValue("TokenCreated", DateTime.UtcNow.ToString("o"), RegistryValueKind.String);
                Console.WriteLine("✅ Token and timestamp saved to registry.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to write to registry: {ex.Message}");
            }
        }

        private static string ReadRegistryValue(string name, string defaultValue)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (key?.GetValue(name) is string value)
                    return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading registry '{name}': {ex.Message}");
            }
            return defaultValue;
        }
    }
}
