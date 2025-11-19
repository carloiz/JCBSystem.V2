using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.Helpers
{
    public static class DataProtectorHelper
    {
        public static async Task<string> Protect(string data)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] encryptedData = ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedData);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.ToString());
            }
        }
         
        public static async Task<string> Unprotect(string encryptedData)
        {
            try
            {
                byte[] dataBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedData = ProtectedData.Unprotect(dataBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.ToString());
            }
        }
    }
}
