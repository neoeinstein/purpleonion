using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Por.Core.Utilities
{
	public static class RSAOperations
	{
		private const string BEGIN_RSA_PRIVATE_KEY = "-----BEGIN RSA PRIVATE KEY-----";
		private const string END_RSA_PRIVATE_KEY = "-----END RSA PRIVATE KEY-----";
		private const int DEFAULT_OPENSSL_LINE_LEN = 64;

		public static RSA FromOpenSslFile(string file)
		{
			StringBuilder sb = new StringBuilder();
			bool begin = false;
			
			foreach (string line in File.ReadAllLines(file))
			{
				if (string.IsNullOrEmpty(line))
				{
					continue;
				}

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

			return FromOpenSslString(sb.ToString());
		}

		public static RSA FromOpenSslString(string key)
		{
			byte[] keyBytes = Convert.FromBase64String(key);
			if (PKCS8.GetType(keyBytes) != PKCS8.KeyInfo.PrivateKey)
			{
				throw new NotSupportedException("Only unencrypted private keys are supported");
			}
			RSA pki = PKCS8.PrivateKeyInfo.DecodeRSA(keyBytes);
			return pki;
		}

		public static void ToOpenSslFile(RSA rsa, string filename)
		{
			string keyString = ToOpenSslString(rsa);
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(BEGIN_RSA_PRIVATE_KEY);

			int curPos;
			for (curPos = 0; 
			     curPos + DEFAULT_OPENSSL_LINE_LEN <= keyString.Length;
			     curPos += DEFAULT_OPENSSL_LINE_LEN)
			{
				sb.AppendLine(keyString.Substring(curPos, DEFAULT_OPENSSL_LINE_LEN));
			}

			if (curPos < keyString.Length)
			{
				sb.AppendLine(keyString.Substring(curPos));
			}

			sb.AppendLine(END_RSA_PRIVATE_KEY);

			File.WriteAllText(filename, sb.ToString());
		}

		public static string ToOpenSslString(RSA rsa)
		{
			byte[] keyBytes = PKCS8.PrivateKeyInfo.Encode(rsa);
			return Convert.ToBase64String(keyBytes);
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
