using System;
using System.IO;
using System.Security.Cryptography;
using Mono.Security;
using Mono.Security.Cryptography;
using Por.Core.Utilities;

namespace Por.Core
{
	public sealed class OnionAddress : IDisposable, IEquatable<OnionAddress>
	{
		public static readonly string KeyFilename = "private_key";
		public static readonly string HostFilename = "hostname";
		public static readonly string Extension = ".onion";
		public static readonly int AddressLength = 16;

		private readonly RSA key;

		private string onion;
		public string Onion
		{
			get
			{
				if (onion == null)
				{
					ASN1 asn = RSAOperations.ToAsn1Key(key);
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
		
		public bool IsPublicOnly
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
			RSA pki = RSAOperations.FromOpenSslString(ssl);
			return new OnionAddress(pki);
		}
		
		public string ToOpenSslString()
		{
			return RSAOperations.ToOpenSslString(key);
		}

		public static OnionAddress ReadFromOnionFile(string file)
		{
			RSA pki = RSAOperations.FromOpenSslFile(file);
			return new OnionAddress(pki);
		}
		
		public void WriteToOnionFiles(string dir)
		{
			if (IsPublicOnly)
			{
				throw new NotSupportedException("Cannot create an onion from a public-only key");
			}
			RSAOperations.ToOpenSslFile(key, Path.Combine(dir, KeyFilename));
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
		
		bool disposed;

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

		public bool Equals(OnionAddress other)
		{
			return other != null && this.Onion.Equals(other.Onion);
		}

		public override bool Equals(object obj)
		{
			OnionAddress other = obj as OnionAddress;
			return other != null && this.Equals(other);
		}

		public override int GetHashCode()
		{
			return this.Onion.GetHashCode();
		}

		public override string ToString()
		{
			return this.Onion;
		}
	}
}
