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

			if (!string.IsNullOrEmpty(settings.CheckDir))
			{
				if (!IsOnionDirectoryValid(settings.CheckDir))
				{
					return 1;
				}
				else
				{
				}
			}
			else if (!string.IsNullOrEmpty(settings.InFilename))
			{
				AttemptToMatchLog();
			}
			else
			{
				using(log = OpenLog())
				{
					AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
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

					if (record.Length != 2)
					{
						continue;
					}

					string logOnion = record[0];
					if (settings.ToMatch != null && settings.ToMatch.IsMatch(logOnion))
					{
						string pkiXml = record[1];
					
						onion = OnionAddress.FromXmlString(pkiXml);

						WriteOnionDirectoryIfMatched(onion);
					}
				}
			}
		}

		private static bool IsOnionDirectoryValid(string dir)
		{
			if (!Directory.Exists(dir))
			{
				Console.Error.WriteLine("Onion directory '" + dir + "' does not exist.");
				return false;
			}

			string onionKeyFile = Path.Combine(dir, OnionAddress.KeyFilename);
			string onionHostFile = Path.Combine(dir, OnionAddress.HostFilename);

			if (!File.Exists(onionKeyFile))
			{
				Console.Error.WriteLine("Onion private key file not found.");
				return false;
			}
			if (!File.Exists(onionHostFile))
			{
				Console.Error.WriteLine("Onion hostname file not found.");
				return false;
			}

			string expectedOnion = File.ReadAllText(onionHostFile).Substring(0,OnionAddress.AddressLength);

			OnionAddress onion = OnionAddress.ReadFromOnionFile(onionKeyFile);

			if (!onion.Onion.Equals(expectedOnion))
			{
				Console.Error.WriteLine("Onion address mismatch:");
				Console.Error.WriteLine("  Expected address: " + expectedOnion);
				Console.Error.WriteLine("  Computed address: " + onion.Onion);
				return false;
			}
			else
			{
				Console.Error.WriteLine("Onion address verified: " + onion.Onion);
				return true;
			}
		}

		private static void WriteOnionDirectoryIfMatched(OnionAddress onion)
		{
			if (settings.ToMatch != null && settings.ToMatch.IsMatch(onion.Onion))
			{
				Console.WriteLine("Found: " + onion.Onion);
				WriteOnionDirectory(onion);
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
				++count;
				OnionAddress priorOnion = OnionAddress.ReadFromOnionFile(privateKeyPath);
				if (OnionAddress.AreKeysSame(priorOnion, onion))
				{
					return null;
				}

				Console.WriteLine("Collision: " + onion.Onion);

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

			UnhandledExceptionHandler(null, null);
#else
			Thread.Sleep(int.MaxValue);
#endif
		}

		private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
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
		}

		private static void ProcessGeneratedOnion(object sender, OnionGenerator.OnionGeneratedEventArgs e)
		{
			if (receivedShutdownSignal)
			{
				e.Cancel = true;
			}

			using (OnionAddress onion = e.Result)
			{
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
