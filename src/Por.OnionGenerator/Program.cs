using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Mono.Security;
using Mono.Security.Cryptography;
using Mono.Unix;
using Mono.Unix.Native;
using log4net;

// TODO: This dependency should be refactored out to another class
using Por.Core;

namespace Por.OnionGenerator
{
	static class Program
	{
		private static readonly ILog Log 
			= LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		private const int CHECK_INTERVAL = 100;
		private static Settings settings = new Settings();
		private static OnionGenerator[] generators;
		
		private static int Main(string[] args)
		{
			log4net.Config.XmlConfigurator.Configure();

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
				Log.Debug("Base directory set to invalid value, setting it to '.'");
				settings.BaseDir = ".";
			}

			if (!string.IsNullOrEmpty(settings.CheckDir))
			{
				Log.DebugFormat("Checking onion directory: {0}", settings.CheckDir);
				try
				{
					OnionDirectory.Validate(settings.CheckDir);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine("Validation error: " + ex.Message);
					return 1;
				}
			}
			else if (!string.IsNullOrEmpty(settings.InFilename))
			{
				Log.DebugFormat("Processing prior run file: {0}", settings.InFilename);
				if (settings.ToMatch == null)
				{
					settings.ShowOptionsError("Reading in prior run (-i) requires a match to test (-m)");
					return 1;
				}
				Log.DebugFormat("Looking for matches to: {0}", settings.ToMatch.ToString());
				OnionLogProcessor processor
					= new OnionLogProcessor(Path.Combine(settings.BaseDir, settings.InFilename),
					                        settings.ToMatch);
				processor.PickDirectory = PickOutputDirectory;
				processor.ProcessLog();
			}
			else
			{
				AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
				GenerateOnions();
				try
				{
					WaitForSignal();
				}
				catch (FileNotFoundException ex)
				{
					UnableToLoadPosix(ex);
				}
				catch (TypeLoadException ex)
				{
					UnableToLoadPosix(ex);
				}
			}
			return 0;
		}

		private static void GenerateOnions()
		{		
			generators = new OnionGenerator[settings.WorkerCount];
			for (int i = 0; i < generators.Length; ++i)
			{
				OnionGenerator o  = new OnionGenerator();
				o.OnionOutputFile = settings.OutFilename;
				o.OnionPattern = settings.ToMatch;
				o.PickDirectory = PickOutputDirectory;
				o.Start();
				generators[i] = o;
			}
		}
		
		private static string PickOutputDirectory(OnionAddress onion)
		{
			string onionDir = Path.Combine(settings.BaseDir, onion.Onion);
			string extension = string.Empty;
			int count = 0;
			while (true)
			{
				using (OnionAddress priorOnion = OnionDirectory.ReadDirectory(onionDir + extension))
				{
					if (priorOnion == null) break;

					if (OnionAddress.AreKeysSame(priorOnion, onion))
					{
						Log.Info("Onion directory already exported, skipping");
						return null;
					}
				}

				Log.WarnFormat("Onion directory collision: {0}", onion.Onion);

				++count;
				extension = "_" + count.ToString();
			}
			return onionDir + extension;
		}

		private static void WaitForSignal()
		{
			using (UnixSignal term = new UnixSignal(Signum.SIGTERM))
			using (UnixSignal ctlc = new UnixSignal(Signum.SIGINT))
			using (UnixSignal hup = new UnixSignal(Signum.SIGHUP))
			{
				UnixSignal[] signals = new UnixSignal[] { term, ctlc, hup };
				while (AreGeneratorsRunning())
				{
					int retVal = UnixSignal.WaitAny(signals, 100);

					if (retVal != CHECK_INTERVAL)
					{
						Log.DebugFormat("Received signal {0}, exiting", signals[retVal].Signum);
						break;
					}
				}
			}
			TerminateAllGenerators();
		}

		private static void UnableToLoadPosix(Exception exception)
		{
			Log.Warn("Unable to catch POSIX signals.", exception);

			while (AreGeneratorsRunning())
			{
				Thread.Sleep(100);
			}
		}

		private static bool AreGeneratorsRunning()
		{
			foreach (OnionGenerator o in generators)
			{
				if (o != null && o.Running)
				{
					return true;
				}
			}
			return false;
		}

		private static void TerminateAllGenerators()
		{
			if (generators != null)
			{
				foreach (OnionGenerator o in generators)
				{
					if (o != null)
					{
						o.Stop();
					}
				}
	
				while (AreGeneratorsRunning())
				{
					Thread.Sleep(0);
				}
			}
		}

		private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Fatal("Unhandled Exception", e.ExceptionObject as Exception);

			TerminateAllGenerators();
		}
	}
}
