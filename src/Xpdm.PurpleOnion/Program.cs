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
					using (OnionAddress onion = OnionAddress.Create())
					{
						if (s.ToMatch.IsMatch(onion.Onion))
						{
							Console.WriteLine("Found: " + onion.Onion);
							string onionDir = Path.Combine(s.BaseDir, onion.Onion);
							Directory.CreateDirectory(onionDir);
							onion.WriteToOnionFiles(onionDir);
							File.WriteAllText(Path.Combine(onionDir, "pki.xml"), onion.ToXmlString(true));
						}

						if (log != null)
						{
							log.WriteLine(string.Format("{0},{1}", onion, onion.ToXmlString(true)));
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
