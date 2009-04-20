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
		public static readonly int AddressLength = 16;

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
					onion = Base32Convert.ToString(hash).Substring(0,AddressLength).ToLowerInvariant();
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

		public string ToXmlString(bool includePrivate)
		{
			return key.ToXmlString(includePrivate);
		}

		public static OnionAddress FromOpenSslString(string ssl)
		{
			RSA pki = RSAExtensions.FromOpenSslString(ssl);
			return new OnionAddress(pki);
		}
		
		public string ToOpenSslString()
		{
			return key.ToOpenSslString();
		}

		public static OnionAddress ReadFromOnionFile(string file)
		{
			RSA pki = RSAExtensions.FromOpenSslFile(file);
			return new OnionAddress(pki);
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

		public static bool AreKeysSame(OnionAddress left, OnionAddress right)
		{
			if (left == null || right == null)
			{
				return false;
			}

			RSAParameters lparam = left.key.ExportParameters(false);
			RSAParameters rparam = right.key.ExportParameters(false);

			if (lparam.Exponent == null || rparam.Exponent == null
			    || lparam.Modulus == null || rparam.Modulus == null
			    || lparam.Exponent.Length != rparam.Exponent.Length 
			    || lparam.Modulus.Length != rparam.Modulus.Length)
			{
				return false;
			}

			for(int i = 0; i < lparam.Exponent.Length; ++i)
			{
				if (lparam.Exponent[i] != rparam.Exponent[i])
				{
					return false;
				}
			}

			for (int i = 0; i < lparam.Modulus.Length; ++i)
			{
				if (lparam.Modulus[i] != rparam.Modulus[i])
				{
					return false;
				}
			}

			return true;
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
