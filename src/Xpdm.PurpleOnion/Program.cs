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
#if POSIX
		private static bool receivedShutdownSignal;
#endif
		private static Settings settings = new Settings();
		private static TextWriter log;
		private static ulong count = 0;
		
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

			try
			{
				if (!string.IsNullOrEmpty(settings.OutFilename))
				{
					Directory.CreateDirectory(settings.BaseDir);
					log = File.AppendText(Path.Combine(settings.BaseDir, settings.OutFilename));
				}
				
				OnionGenerator[] generators = new OnionGenerator[settings.WorkerCount];
				for (int i = 0; i < generators.Length; ++i)
				{
					generators[i] = new OnionGenerator();
					generators[i].OnionGenerated += ProcessGeneratedOnion;
					generators[i].StartGenerating();
				}

#if POSIX
				UnixSignal term = new UnixSignal(Signum.SIGTERM);
				UnixSignal ctlc = new UnixSignal(Signum.SIGINT);
				UnixSignal hup = new UnixSignal(Signum.SIGHUP);

				UnixSignal.WaitAny(new UnixSignal[] { term, ctlc, hup });

				receivedShutdownSignal = true;
				
				bool ready = false;
				while (!ready)
				{
					ready = true;
					foreach (OnionGenerator o in generators)
					{
						if (!o.Stopped)
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
				return 0;
			}
			finally
			{
				if (log != null)
				{
					log.Dispose();
				}
			}
		}

		private static void ProcessGeneratedOnion(object sender, OnionGenerator.OnionGeneratedEventArgs e)
		{
#if POSIX
			if (receivedShutdownSignal)
			{
				e.Cancel = true;
			}
#endif
			using (OnionAddress onion = e.Result)
			{
				if (settings.ToMatch.IsMatch(onion.Onion))
				{
					Console.WriteLine("Found: " + onion.Onion);
					string onionDir = Path.Combine(settings.BaseDir, onion.Onion);
					Directory.CreateDirectory(onionDir);
					onion.WriteToOnionFiles(onionDir);
					File.WriteAllText(Path.Combine(onionDir, "pki.xml"), onion.ToXmlString(true));
				}

				if (log != null)
				{
					log.WriteLine(string.Format("{0},{1}", onion.Onion, onion.ToXmlString(true)));
					log.Flush();
				}

				Console.Write(onion.Onion + " " + ++count + "\r");
			}
		}
	}
}
