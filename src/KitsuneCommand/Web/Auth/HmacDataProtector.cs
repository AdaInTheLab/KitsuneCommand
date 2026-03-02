using System.Security.Cryptography;
using Microsoft.Owin.Security.DataProtection;

namespace KitsuneCommand.Web.Auth
{
    /// <summary>
    /// HMAC-SHA256 based data protector for Mono/Unity environments
    /// where DPAPI is not available.
    /// </summary>
    public class HmacDataProtectionProvider : IDataProtectionProvider
    {
        private readonly byte[] _key;

        public HmacDataProtectionProvider(string appName)
        {
            // Derive a stable key from the app name
            using var sha = SHA256.Create();
            _key = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(appName + "_KitsuneCommand_TokenKey"));
        }

        public IDataProtector Create(params string[] purposes)
        {
            return new HmacDataProtector(_key, purposes);
        }
    }

    public class HmacDataProtector : IDataProtector
    {
        private readonly byte[] _key;

        public HmacDataProtector(byte[] key, string[] purposes)
        {
            // Derive a purpose-specific key
            using var sha = SHA256.Create();
            var combined = System.Text.Encoding.UTF8.GetBytes(string.Join(".", purposes));
            var input = new byte[key.Length + combined.Length];
            Buffer.BlockCopy(key, 0, input, 0, key.Length);
            Buffer.BlockCopy(combined, 0, input, key.Length, combined.Length);
            _key = sha.ComputeHash(input);
        }

        public byte[] Protect(byte[] userData)
        {
            using var hmac = new HMACSHA256(_key);
            var mac = hmac.ComputeHash(userData);
            // Output: [mac (32 bytes)] [data]
            var result = new byte[32 + userData.Length];
            Buffer.BlockCopy(mac, 0, result, 0, 32);
            Buffer.BlockCopy(userData, 0, result, 32, userData.Length);
            return result;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            if (protectedData.Length < 32)
                throw new CryptographicException("Invalid protected data");

            var mac = new byte[32];
            var data = new byte[protectedData.Length - 32];
            Buffer.BlockCopy(protectedData, 0, mac, 0, 32);
            Buffer.BlockCopy(protectedData, 32, data, 0, data.Length);

            using var hmac = new HMACSHA256(_key);
            var expectedMac = hmac.ComputeHash(data);

            if (!ConstantTimeEquals(mac, expectedMac))
                throw new CryptographicException("Data integrity check failed");

            return data;
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
