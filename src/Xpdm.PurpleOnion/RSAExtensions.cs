using System;
using System.Security.Cryptography;
using System.Text;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Xpdm.PurpleOnion
{
	static class RSAExtensions
	{
		private const string BEGIN_RSA_PRIVATE_KEY = "-----BEGIN RSA PRIVATE KEY-----";
		private const string END_RSA_PRIVATE_KEY = "-----END RSA PRIVATE KEY-----";

		public static RSA FromDecryptedOpenSslString(string keyin)
		{
			keyin = keyin.Trim();
			StringBuilder sb = new StringBuilder();
			bool begin = false;
			
			foreach (string line in keyin.Split(new char[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
			{
				if (!begin && line == BEGIN_RSA_PRIVATE_KEY)
				{
					begin = true;
				}
				else if (begin)
				{
					if (line == END_RSA_PRIVATE_KEY)
					{
						break;
					}
					sb.Append(line);
				}
			}
			
			byte[] key = Convert.FromBase64String(sb.ToString());
			
			RSA pki = PKCS8.PrivateKeyInfo.DecodeRSA(key);
			return pki;
		}

		public static ASN1 ToAsn1(RSA rsa)
		{
			ASN1 asn = new ASN1(0x30);
			ASN1 asnOid = new ASN1(0x30);
			
			// {iso(1) member-body(2) us(840) rsadsi(113549) pkcs(1) pkcs-1(1) rsaEncryption(1)}
			// http://www.oid-info.com/get/1.2.840.113549.1.1.1
			asnOid.Add(ASN1Convert.FromOid("1.2.840.113549.1.1.1"));
			
			asnOid.Add(new ASN1(0x05));
			asn.Add(asnOid);
			
			ASN1 asnBits = new ASN1(0x03, new byte[1]);
			byte[] intermediate = ToAsn1Key(rsa).GetBytes();
			byte[] key = new byte[intermediate.Length + 1];
			intermediate.CopyTo(key, 1);
			asnBits.Value = key;
			
			asn.Add(asnBits);
			
			return asn;
		}

		public static ASN1 ToAsn1Key(RSA rsa)
		{
			RSAParameters parameters = rsa.ExportParameters(false);
			
			ASN1 asnKey = new ASN1(0x30);
			asnKey.Add(ASN1Convert.FromUnsignedBigInteger(parameters.Modulus));
			asnKey.Add(ASN1Convert.FromUnsignedBigInteger(parameters.Exponent));
			return asnKey;
		}
	
	}
}
