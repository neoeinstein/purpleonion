using System;
using System.IO;
using System.Security.Cryptography;
using Mono.Security;
using Mono.Security.Cryptography;

namespace Xpdm.PurpleOnion
{
	static class Program
	{
		private static int Main(string[] args)
		{
			Settings s = new Settings();

			if(!s.TryParse(args))
			{
				return 1;
			}

			if (s.ShouldShowHelp)
			{
				s.ShowHelp(Console.Out);
				return 0;
			}

			if (string.IsNullOrEmpty(s.BaseDir))
			{
				s.BaseDir = ".";
			}

			TextWriter log = null;
			try
			{
				if (!string.IsNullOrEmpty(s.OutFilename))
				{
					Directory.CreateDirectory(s.BaseDir);
					log = File.AppendText(Path.Combine(s.BaseDir, s.OutFilename));
				}

				long count = 0;
				while (true)
				{
					using (RSA pki = RSA.Create())
					{
						ASN1 asn = RSAExtensions.ToAsn1Key(pki);
						byte[] hash = SHA1CryptoServiceProvider.Create().ComputeHash(asn.GetBytes());
						string onion = ConvertExtensions.FromBytesToBase32String(hash).Substring(0,16).ToLowerInvariant();
						if (s.ToMatch.IsMatch(onion))
						{
							Console.WriteLine("Found: " + onion);
							Directory.CreateDirectory(s.BaseDir);
							string onionDir = Path.Combine(s.BaseDir, onion);
							Directory.CreateDirectory(onionDir);
							File.WriteAllText(Path.Combine(onionDir, "pki.xml"), pki.ToXmlString(true));
							File.WriteAllText(Path.Combine(onionDir, "private_key"), System.Convert.ToBase64String(PKCS8.PrivateKeyInfo.Encode(pki)));
							File.WriteAllText(Path.Combine(onionDir, "hostname"), onion + ".onion");
						}

						if (log != null)
						{
							log.WriteLine(string.Format("{0},{1}", onion, pki.ToXmlString(true)));
							log.Flush();
						}

						Console.Write(onion + " " + ++count + "\r");
					}
				}
			}
			finally
			{
				if (log != null)
				{
					log.Dispose();
				}
			}
		}
	}
}
