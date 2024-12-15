using System.Security.Cryptography;
using System.Text;

namespace SefCloud.Backend.Services
{
    public class EncryptionService
    {
        public string Encrypt(string value, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                byte[] iv = new byte[16];
                aes.IV = iv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(value);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    byte[] result = new byte[iv.Length + cipherBytes.Length];
                    Array.Copy(iv, 0, result, 0, iv.Length);
                    Array.Copy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

                    string base64Result = Convert.ToBase64String(result);

                    string safeBase64Result = base64Result.Replace('+', '-').Replace('/', '_').Replace('=', ',');

                    return safeBase64Result;
                }
            }
        }


        public string Decrypt(string safeBase64EncryptedValue, string key)
        {
            string base64EncryptedValue = safeBase64EncryptedValue.Replace('-', '+').Replace('_', '/').Replace(',', '=');
            byte[] combinedBytes = Convert.FromBase64String(base64EncryptedValue);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;

                byte[] iv = new byte[16];
                Array.Copy(combinedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                byte[] cipherBytes = new byte[combinedBytes.Length - iv.Length];
                Array.Copy(combinedBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }


        public string GenerateEncryptionKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var key = new byte[32];
                rng.GetBytes(key);
                return Convert.ToBase64String(key);
            }
        }


        public string DecryptFileName(string encryptedFileName, string encryptionKey)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(encryptedFileName);
            string fileExtension = Path.GetExtension(encryptedFileName);
            string decryptedName = this.Decrypt(fileNameWithoutExtension, encryptionKey);
            return decryptedName + fileExtension;
        }
    }
}
