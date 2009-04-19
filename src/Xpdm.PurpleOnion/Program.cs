using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Mono.Security;
using Mono.Security.Cryptography;
#if POSIX
using Mono.Unix;
using Mono.Unix.Native;
#endif

namespace Xpdm.PurpleOnion
{
	static class Program
	{
		private const char LOG_FIELD_SEPARATOR = ',';
		private static bool receivedShutdownSignal;
		private static Settings settings = new Settings();
		private static TextWriter log;
		private static ulong count = 0;
		private static OnionGenerator[] generators;

		
		private static int Main(string[] args)
		{
			if(!settings.TryParse(args))
			{
				return 1;
			}

			if (settings.ShouldShowHelp)
			{
				settings.ShowHelp(Console.Out);
				return 0;
			}

			if (string.IsNullOrEmpty(settings.BaseDir))
			{
				settings.BaseDir = ".";
			}

			if (!string.IsNullOrEmpty(settings.InFilename))
			{
				AttemptToMatchLog();
			}
			else
			{
				using(log = OpenLog())
				{
					GenerateOnions();
					WaitForSignal();
				}
			}
			return 0;
		}

		private static TextWriter OpenLog()
		{
			if (!string.IsNullOrEmpty(settings.OutFilename))
			{
				Directory.CreateDirectory(settings.BaseDir);
				return File.AppendText(Path.Combine(settings.BaseDir, settings.OutFilename));
			}
			return null;
		}

		private static void GenerateOnions()
		{		
			generators = new OnionGenerator[settings.WorkerCount];
			for (int i = 0; i < generators.Length; ++i)
			{
				generators[i] = new OnionGenerator();
				generators[i].OnionGenerated += ProcessGeneratedOnion;
				generators[i].Start();
			}
		}
		
		private static void AttemptToMatchLog()
		{
			OnionAddress onion;
			using (StreamReader file = File.OpenText(Path.Combine(settings.BaseDir, settings.InFilename)))
			{
				while (!file.EndOfStream)
				{
					string line = file.ReadLine().Trim();
					if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
					{
						continue;
					}
					
					string[] record = line.Split(LOG_FIELD_SEPARATOR);
					Console.Write(record[0]);
					string pkiXml = record[1];
					
					onion = OnionAddress.FromXmlString(pkiXml);

					Console.WriteLine(" " + onion.Onion);
					
					if (settings.ToMatch.IsMatch(onion.Onion))
					{
						WriteOnionDirectory(onion);
					}
				}
			}
		}

		private static void WriteOnionDirectory(OnionAddress onion)
		{
			string onionDir = PickOnionDirectory(onion);
			if (!string.IsNullOrEmpty(onionDir))
			{
				Directory.CreateDirectory(onionDir);
				onion.WriteToOnionFiles(onionDir);
			}
		}

		private static string PickOnionDirectory(OnionAddress onion)
		{
			string onionDir = Path.Combine(settings.BaseDir, onion.Onion);	
			string privateKeyPath = Path.Combine(onionDir, OnionAddress.KeyFilename);
			string extension = string.Empty;
			int count = 0;
			while (File.Exists(privateKeyPath))
			{
				string currentKey = File.ReadAllText(privateKeyPath);
				if (currentKey == onion.ToOpenSslString())
				{
					return null;
				}
				if (count == 0)
				{
					Console.WriteLine("Collision found for onion address: " + onion.Onion);
				}
				++count;
				extension = "_" + count.ToString();
				privateKeyPath = Path.Combine(onionDir + extension, OnionAddress.KeyFilename);
			}
			return onionDir + extension;
		}

		private static void WaitForSignal()
		{
#if POSIX
			UnixSignal term = new UnixSignal(Signum.SIGTERM);
			UnixSignal ctlc = new UnixSignal(Signum.SIGINT);
			UnixSignal hup = new UnixSignal(Signum.SIGHUP);

			UnixSignal.WaitAny(new UnixSignal[] { term, ctlc, hup });

			receivedShutdownSignal = true;

			foreach (OnionGenerator o in generators)
			{
				o.Stop();
			}

			bool ready = false;
			while (!ready)
			{
				ready = true;
				foreach (OnionGenerator o in generators)
				{
					if (o.Running)
					{
						ready = false;
					}
				}

				Thread.Sleep(0);
			}

			Console.WriteLine("");
			Console.WriteLine("Closed cleanly");
#else
			Thread.Sleep(int.MaxValue);
#endif
		}

		private static void ProcessGeneratedOnion(object sender, OnionGenerator.OnionGeneratedEventArgs e)
		{
			if (receivedShutdownSignal)
			{
				e.Cancel = true;
			}

			using (OnionAddress onion = e.Result)
			{
				if (settings.ToMatch.IsMatch(onion.Onion))
				{
					Console.WriteLine("Found: " + onion.Onion);
					WriteOnionDirectory(onion);
				}

				if (log != null)
				{
					log.WriteLine(string.Format("{0}{1}{2}", onion.Onion, LOG_FIELD_SEPARATOR, onion.ToXmlString(true)));
					log.Flush();
				}

				Console.Write(onion.Onion + " " + ++count + "\r");
			}
		}
	}
}
