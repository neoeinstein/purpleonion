using System;
using System.IO;
using System.Security.Cryptography;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Xpdm.PurpleOnion
{
	sealed class OnionAddress : IDisposable
	{
		private const string DOT_ONION = ".onion";
		private readonly RSA key;
		
		private string onion = null;
		public string Onion
		{
			get
			{
				if (onion == null)
				{
					ASN1 asn = key.ToAsn1Key();
					byte[] hash;
					using (SHA1 hasher = SHA1.Create())
					{
						hash = hasher.ComputeHash(asn.GetBytes());
					}
					onion = Base32Convert.ToString(hash).Substring(0,16).ToLowerInvariant();
				}
				return onion;
			}
		}
		
		private bool IsPublicOnly
		{
			get
			{
				return key.ExportParameters(true).P == null;
			}
		}
		
		private OnionAddress(RSA key)
		{
			this.key = key;
		}
		
		public static OnionAddress Create()
		{
			RSA pki = RSA.Create();
			OnionAddress retVal = new OnionAddress(pki);
			return retVal;
		}
		
		public static OnionAddress FromXmlString(string xml)
		{
			RSA pki = RSA.Create();
			pki.FromXmlString(xml);
			OnionAddress retVal = new OnionAddress(pki);
			return retVal;
		}
		
		public string ToXmlString(bool includePrivate)
		{
			return key.ToXmlString(includePrivate);
		}
		
		public static OnionAddress ReadFromOnionFile(string file)
		{
			string openSslString = File.ReadAllText(file);
			byte[] openSslKey = Convert.FromBase64String(openSslString);
			if (PKCS8.GetType(openSslKey) != PKCS8.KeyInfo.PrivateKey)
			{
				throw new NotSupportedException("Only unencrypted private keys are supported");
			}
			RSA pki = PKCS8.PrivateKeyInfo.DecodeRSA(openSslKey);
			return new OnionAddress(pki);
		}
		
		public void WriteToOnionFiles(string dir)
		{
			if (IsPublicOnly)
			{
				throw new NotSupportedException("Cannot create an onion from a public-only key");
			}
			File.WriteAllText(Path.Combine(dir, "private_key"),
			                  Convert.ToBase64String(PKCS8.PrivateKeyInfo.Encode(key)));
			File.WriteAllText(Path.Combine(dir, "hostname"), Onion + DOT_ONION);
		}
		
		bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposed)
			{
				return;
			}
			if (disposing)
			{
				if (key != null)
				{
					((IDisposable)key).Dispose();
				}
			}
			disposed = true;
		}

	}
}
