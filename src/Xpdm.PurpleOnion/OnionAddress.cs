using System;
using System.IO;
using System.Security.Cryptography;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Xpdm.PurpleOnion
{
	sealed class OnionAddress : IDisposable
	{
		public static readonly string KeyFilename = "private_key";
		public static readonly string HostFilename = "hostname";
		public static readonly string Extension = ".onion";

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
			RSA pki = new RSACryptoServiceProvider();
			pki.FromXmlString(xml);
			OnionAddress retVal = new OnionAddress(pki);
			return retVal;
		}

//		public void FromXmlString(string xml)
//		{
//			RSA pki = new RSACryptoServiceProvider();
//			pki.FromXmlString(xml);
//			key.Clear();
//			key = pki;
//		}
		
		public string ToXmlString(bool includePrivate)
		{
			return key.ToXmlString(includePrivate);
		}

		public static OnionAddress FromOpenSslString(string ssl)
		{
			RSA pki = RSAExtensions.FromOpenSslString(ssl);
			return new OnionAddress(pki);
		}
		
//		public void FromOpenSslString(string ssl)
//		{
//			RSA pki = RSAExtensions.FromOpenSslString(ssl);
//			key.Clear();
//			key = pki;
//		}

		public string ToOpenSslString()
		{
			return key.ToOpenSslString();
		}

		public static OnionAddress ReadFromOnionFile(string file)
		{
			string openSslString = File.ReadAllText(file);
			return FromOpenSslString(openSslString);
		}
		
		public void WriteToOnionFiles(string dir)
		{
			if (IsPublicOnly)
			{
				throw new NotSupportedException("Cannot create an onion from a public-only key");
			}
			key.ToOpenSslFile(Path.Combine(dir, KeyFilename));
			File.WriteAllText(Path.Combine(dir, HostFilename), Onion + Extension + "\n");
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
				IDisposable disKey = key as IDisposable;
				if (disKey != null)
				{
					disKey.Dispose();
				}
			}
			disposed = true;
		}

	}
}
