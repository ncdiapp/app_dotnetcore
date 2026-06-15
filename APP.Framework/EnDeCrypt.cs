
using System;
using System.IO;
using System.Security.Cryptography;

namespace APP.Framework
{
    // FIPS-compliant encryption/decryption using AesCryptoServiceProvider (Windows CSP)
    // and Rfc2898DeriveBytes (PBKDF2/HMACSHA1) instead of the legacy PasswordDeriveBytes (MD5).
    //
    // IMPORTANT: Existing data encrypted with the old EnDeCrypt cannot be decrypted with this
    // version because PasswordDeriveBytes and Rfc2898DeriveBytes produce different keys from the
    // same password. Perform a one-time re-encryption migration before enabling FIPS mode.
    public class EnDeCrypt
    {
        private static readonly byte[] Salt = { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        private const int Iterations = 1000;

        private static void DeriveKeyAndIV(string password, out byte[] key, out byte[] iv)
        {
            using (var pdb = new Rfc2898DeriveBytes(password, Salt, Iterations))
            {
                key = pdb.GetBytes(32);
                iv  = pdb.GetBytes(16);
            }
        }

        private static byte[] Encrypt(byte[] clearData, byte[] key, byte[] iv)
        {
            using (var ms = new MemoryStream())
            using (var alg = new AesCryptoServiceProvider())
            {
                alg.Key = key;
                alg.IV  = iv;
                using (var cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearData, 0, clearData.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }

        public static string Encrypt(string clearText, string password)
        {
            byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);
            DeriveKeyAndIV(password, out byte[] key, out byte[] iv);
            return Convert.ToBase64String(Encrypt(clearBytes, key, iv));
        }

        public static byte[] Encrypt(byte[] clearData, string password)
        {
            DeriveKeyAndIV(password, out byte[] key, out byte[] iv);
            return Encrypt(clearData, key, iv);
        }

        public static void Encrypt(string fileIn, string fileOut, string password)
        {
            DeriveKeyAndIV(password, out byte[] key, out byte[] iv);
            using (var fsIn  = new FileStream(fileIn,  FileMode.Open,         FileAccess.Read))
            using (var fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write))
            using (var alg   = new AesCryptoServiceProvider())
            {
                alg.Key = key;
                alg.IV  = iv;
                using (var cs = new CryptoStream(fsOut, alg.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    var buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                        cs.Write(buffer, 0, bytesRead);
                    cs.FlushFinalBlock();
                }
            }
        }

        private static byte[] Decrypt(byte[] cipherData, byte[] key, byte[] iv)
        {
            using (var ms = new MemoryStream())
            using (var alg = new AesCryptoServiceProvider())
            {
                alg.Key = key;
                alg.IV  = iv;
                using (var cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherData, 0, cipherData.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            DeriveKeyAndIV(password, out byte[] key, out byte[] iv);
            byte[] decryptedData = Decrypt(cipherBytes, key, iv);
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }

        public static byte[] Decrypt(byte[] cipherData, string password)
        {
            DeriveKeyAndIV(password, out byte[] key, out byte[] iv);
            return Decrypt(cipherData, key, iv);
        }

        public static void Decrypt(string fileIn, string fileOut, string password)
        {
            DeriveKeyAndIV(password, out byte[] key, out byte[] iv);
            using (var fsIn  = new FileStream(fileIn,  FileMode.Open,         FileAccess.Read))
            using (var fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write))
            using (var alg   = new AesCryptoServiceProvider())
            {
                alg.Key = key;
                alg.IV  = iv;
                using (var cs = new CryptoStream(fsOut, alg.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    var buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                        cs.Write(buffer, 0, bytesRead);
                    cs.FlushFinalBlock();
                }
            }
        }
    }
}
