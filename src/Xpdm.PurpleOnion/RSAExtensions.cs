using System.Security.Cryptography;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Xpdm.PurpleOnion
{
	class RSAExtensions
	{
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
