namespace JCBSystem.Core.common.Helpers
{
    public class PasswordHelper
    {
        // Hashing ng password gamit ang BCrypt
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Pag-verify ng password
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
