
using System;
using System.Security.Cryptography;
namespace App.BL
{
    public static class AppSecurityPasswordHashBL

	{

		private const int SaltByteSize = 24;
		private const int HashByteSize = 32;
		private const int Pbkdf2Iterations = 600000;
		private const int IterationIndex = 0;
		private const int SaltIndex = 1;
		private const int Pbkdf2Index = 2;

		public static string HashPassword(string password)
		{
			var cryptoProvider = new RNGCryptoServiceProvider();
			byte[] salt = new byte[SaltByteSize];
			cryptoProvider.GetBytes(salt);

			var hash = GetPbkdf2Bytes(password, salt, Pbkdf2Iterations, HashByteSize);
			return Pbkdf2Iterations + ":" +
				   Convert.ToBase64String(salt) + ":" +
				   Convert.ToBase64String(hash);
		}

		public static bool ValidatePassword(string password, string correctHash)
		{
			char[] delimiter = { ':' };
			var split = correctHash.Split(delimiter);
			var iterations = Int32.Parse(split[IterationIndex]);
			var salt = Convert.FromBase64String(split[SaltIndex]);
			var hash = Convert.FromBase64String(split[Pbkdf2Index]);

			var testHash = GetPbkdf2Bytes(password, salt, iterations, hash.Length);
			return SlowEquals(hash, testHash);
		}

		private static bool SlowEquals(byte[] a, byte[] b)
		{
			var diff = (uint)a.Length ^ (uint)b.Length;
			for (int i = 0; i < a.Length && i < b.Length; i++)
			{
				diff |= (uint)(a[i] ^ b[i]);
			}
			return diff == 0;
		}

		private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputBytes)
		{
			var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
			return pbkdf2.GetBytes(outputBytes);
		}



	}



}

//Rfc2898DeriveBytes takes a password, a salt, and an iteration count, and then generates keys through calls to the GetBytes method.

//RFC 2898 includes methods for creating a key and initialization vector(IV) from a password and salt.You can use PBKDF2, a password-based key derivation function, to derive keys using a pseudo-random function that allows keys of virtually unlimited length to be generated.The Rfc2898DeriveBytes class can be used to produce a derived key from a base key and other parameters.In a password-based key derivation function, the base key is a password and the other parameters are a salt value and an iteration count.


//For more information about PBKDF2, see RFC 2898, "PKCS #5: Password-Based Cryptography Specification Version 2.0," available on the Request for Comments Web site.See section 5.2, "PBKDF2," for complete details.