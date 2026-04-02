namespace KitsuneCommand.Web.Auth
{
    /// <summary>
    /// BCrypt-based password hashing. Replaces the original ServerKit's plaintext password storage.
    /// </summary>
    public static class PasswordHasher
    {
        private const int WorkFactor = 12;

        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
        }

        public static bool Verify(string password, string hash)
        {
            try
            {
                // BCrypt.Net.BCrypt.Verify is broken under Unity's Mono runtime.
                // Instead, re-hash with the stored hash as salt and compare strings.
                var rehashed = BCrypt.Net.BCrypt.HashPassword(password, hash);
                return string.Equals(rehashed, hash, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
