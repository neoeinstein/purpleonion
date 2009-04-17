using System;
using System.IO;
using System.Security.Cryptography;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Xpdm.PurpleOnion
{
	class Program
	{
		private static void Main()
		{
			long count = 0;
			while (true)
			{
				RSA pki = RSA.Create();
				ASN1 asn = RSAExtensions.ToAsn1Key(pki);
				byte[] hash = SHA1CryptoServiceProvider.Create().ComputeHash(asn.GetBytes());
				string onion = ConvertExtensions.FromBytesToBase32String(hash).Substring(0,16).ToLowerInvariant();
				if (onion.Contains("tor") || onion.Contains("mirror"))
				{
					Console.WriteLine("Found: " + onion);
					Directory.CreateDirectory(onion);
					File.WriteAllText(Path.Combine(onion, "pki.xml"), pki.ToXmlString(true));
					File.WriteAllText(Path.Combine(onion, "private_key"), System.Convert.ToBase64String(PKCS8.PrivateKeyInfo.Encode(pki)));
					File.WriteAllText(Path.Combine(onion, "hostname"), onion + ".onion");
				}

				Console.WriteLine(onion + " " + ++count);
			}
		}
	}
}
