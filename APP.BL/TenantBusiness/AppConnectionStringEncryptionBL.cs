using System;
using System.Configuration;
using APP.Framework;
using System.Security.Cryptography;
using System.Text;

namespace App.BL
{
    /// <summary>
    /// AES-256 encryption for tenant connection strings stored in AppDataSourceRegister.
    /// Encrypted values are prefixed with "AES:" so unencrypted legacy values are still
    /// accepted transparently (Decrypt returns plaintext as-is when no prefix is found).
    /// Key is stored in appSettings["AppConnectionStringEncryptionKey"].
    /// </summary>
    public static class AppConnectionStringEncryptionBL
    {
        private const string EncryptedPrefix = "AES:";

        private static byte[] GetKey()
        {
            string raw = AppConfig.Get("AppConnectionStringEncryptionKey") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "AppConnectionStringEncryptionKey is not configured in appSettings.");
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;
            if (plainText.StartsWith(EncryptedPrefix))
                return plainText; // already encrypted

            byte[] key = GetKey();
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                using (var enc = aes.CreateEncryptor())
                {
                    byte[] plain = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
                    // Prepend IV so Decrypt can retrieve it
                    byte[] payload = new byte[aes.IV.Length + cipher.Length];
                    Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipher, 0, payload, aes.IV.Length, cipher.Length);
                    return EncryptedPrefix + Convert.ToBase64String(payload);
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;
            if (!cipherText.StartsWith(EncryptedPrefix))
                return cipherText; // legacy plaintext — pass through

            byte[] key = GetKey();
            byte[] payload = Convert.FromBase64String(cipherText.Substring(EncryptedPrefix.Length));
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] cipher = new byte[payload.Length - iv.Length];
                Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(payload, iv.Length, cipher, 0, cipher.Length);
                aes.IV = iv;
                using (var dec = aes.CreateDecryptor())
                {
                    byte[] plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                    return Encoding.UTF8.GetString(plain);
                }
            }
        }

        public static bool IsEncrypted(string value) =>
            !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix);
    }
}
