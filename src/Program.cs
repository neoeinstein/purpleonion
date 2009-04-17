using System.IO;
using System.Security.Cryptography;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Xpdm.PurpleOnion
{
	class Program
	{
		private static byte[] GenerateAsn(RSA rsa)
		{
			RSAParameters parameters = rsa.ExportParameters(false);
			ASN1 asn = new ASN1(0x30);
			ASN1 asnOid = new ASN1(0x30);
			
			// {iso(1) member-body(2) us(840) rsadsi(113549) pkcs(1) pkcs-1(1) rsaEncryption(1)}
			// http://www.oid-info.com/get/1.2.840.113549.1.1.1
			asnOid.Add(ASN1Convert.FromOid("1.2.840.113549.1.1.1"));
			
			asnOid.Add(new ASN1(0x05));
			asn.Add(asnOid);
			
			ASN1 asnBits = new ASN1(0x03, new byte[1]);
			ASN1 asnKey = new ASN1(0x30);
			asnKey.Add(ASN1Convert.FromUnsignedBigInteger(parameters.Modulus));
			asnKey.Add(ASN1Convert.FromUnsignedBigInteger(parameters.Exponent));
			byte[] intermediate = asnKey.GetBytes();
			byte[] key = new byte[intermediate.Length + 1];
			intermediate.CopyTo(key, 1);
			asnBits.Value = key;
			
			asn.Add(asnBits);
			
			return asnKey.GetBytes();
		}
	
		private static void Main()
		{
			long count = 0;
			while (true)
			{
				RSA pki = RSA.Create();
				byte[] asn = GenerateAsn(pki);
				byte[] hash = SHA1CryptoServiceProvider.Create().ComputeHash(asn);
				string onion = ConvertExtensions.FromBytesToBase32String(hash).Substring(0,16).ToLowerInvariant();
				if (onion.Contains("tor") || onion.Contains("mirror"))
				{
					System.Console.WriteLine("Found: " + onion);
					Directory.CreateDirectory(onion);
					File.WriteAllText(Path.Combine(onion, "pki.xml"), pki.ToXmlString(true));
					File.WriteAllText(Path.Combine(onion, "private_key"), System.Convert.ToBase64String(PKCS8.PrivateKeyInfo.Encode(pki)));
					File.WriteAllText(Path.Combine(onion, "hostname"), onion + ".onion");
				}

				System.Console.WriteLine(onion + " " + ++count);
			}
		}
	}
}
