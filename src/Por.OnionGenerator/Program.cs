using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Mono.Security;
using Mono.Security.Cryptography;
using Mono.Unix;
using Mono.Unix.Native;
using log4net;
using log4net.Appender;
using log4net.Layout;

// TODO: This dependency should be refactored out to another class
using Por.Core;

namespace Por.OnionGenerator
{
	static class Program
	{
		private static readonly ILog Log 
			= LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		private const int CHECK_INTERVAL = 100;
		
		private static int Main(string[] args)
		{
			log4net.Config.XmlConfigurator.Configure();
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

			Settings settings = new Settings();
			
			if(!settings.TryParse(args))
			{
				return 1;
			}

			if (settings.ShouldShowHelp)
			{
				settings.ShowHelp(Console.Out);
				return 0;
			}

			ValidateBaseDirSetting(settings);

			Environment.CurrentDirectory = settings.BaseDir;

			return RunMainProgram(settings);
		}

		private static void ValidateBaseDirSetting(Settings settings)
		{
			if (string.IsNullOrEmpty(settings.BaseDir))
			{
				Log.Debug("Base directory set to invalid value, setting it to '.'");
				settings.BaseDir = ".";
			}
		}

		private static int RunMainProgram(Settings settings)
		{
			int retVal;
			if (!string.IsNullOrEmpty(settings.CheckDir))
			{
				retVal = CheckOnionDirectory(settings);
			}
			else if (!string.IsNullOrEmpty(settings.InFilename))
			{
				retVal = ProcessPriorLog(settings);
			}
			else
			{
				retVal = GenerateOnions(settings);
			}
			return retVal;
		}

		private static int CheckOnionDirectory(Settings settings)
		{
			Log.DebugFormat("Checking onion directory: {0}", settings.CheckDir);
			try
			{
				OnionDirectory.Validate(settings.CheckDir);
			}
			catch (IOException ex)
			{
				Console.Error.WriteLine("Validation error (something missing): " + ex.Message);
				return 1;
			}
			catch (PorException ex)
			{
				Console.Error.WriteLine("Validation error: " + ex.Message);
				return 1;
			}
			return 0;
		}

		private static int ProcessPriorLog(Settings settings)
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
				                        settings.ToMatch) {
				PickDirectory = PickOutputDirectory,
				MatchMax = settings.MaxMatch,
			};
			processor.ProcessLog();

			return 0;
		}

		private static int GenerateOnions(Settings settings)
		{
			OnionGenerator[] generators = null;
			try
			{
				IAppender appender = GetOnionLoggingAppender(settings);
				
				generators = PrepareGenerators(settings, appender);

				foreach (OnionGenerator g in generators)
				{
					g.Start();
				}

				bool noPosix = false;
				try
				{
					WaitForSignal(generators);
				}
				catch (FileNotFoundException ex)
				{
					LogNoPosixMessage(ex);
					noPosix = true;
				}
				catch (TypeLoadException ex)
				{
					LogNoPosixMessage(ex);
					noPosix = true;
				}

				if (noPosix)
				{
					NonPosixWait(generators);
				}
			}
			finally
			{
				TerminateAllGenerators(generators);

				if (generators != null)
				{
					for (int i = 0; i < generators.Length; ++i)
					{
						if (generators[i] != null)
						{
							generators[i].Dispose();
							generators[i] = null;
						}
					}
				}
			}

			return 0;
		}

		private static void LogNoPosixMessage(Exception ex)
		{
			string message = "Unable to catch POSIX signals";
			if (System.Environment.OSVersion.Platform == PlatformID.Unix
			    || System.Environment.OSVersion.Platform == PlatformID.MacOSX)
			{
				Log.Warn(message, ex);
			}
			else
			{
				Log.Debug(message + " (expected)");
			}
		}

		private static IAppender GetOnionLoggingAppender(Settings settings)
		{
			if(string.IsNullOrEmpty(settings.OutFilename))
			{
				return null;
			}
			
			FileAppender appender = new FileAppender {
				Layout = new PatternLayout("%m%n"),
				Encoding = System.Text.Encoding.ASCII,
				AppendToFile = true,
				File = settings.OutFilename,
				Name = "OnionLog",
			};
			appender.ActivateOptions();

			return appender;
		}

		private static OnionGenerator[] PrepareGenerators(Settings settings, IAppender appender)
		{		
			OnionGenerator[] generators = new OnionGenerator[settings.WorkerCount];
			for (int i = 0; i < generators.Length; ++i)
			{
				generators[i] = new OnionGenerator {
					OnionPattern = settings.ToMatch,
					PickDirectory = PickOutputDirectory,
					GenerateMax = settings.MaxGenerate,
					MatchMax = settings.MaxMatch,
				};
				generators[i].OnionAppender.AddAppender(appender);
			}
			return generators;
		}
		
		private static void WaitForSignal(OnionGenerator[] generators)
		{
			using (UnixSignal term = new UnixSignal(Signum.SIGTERM))
			using (UnixSignal ctlc = new UnixSignal(Signum.SIGINT))
			using (UnixSignal hup = new UnixSignal(Signum.SIGHUP))
			{
				UnixSignal[] signals = new UnixSignal[] { term, ctlc, hup };
				while (AreGeneratorsRunning(generators))
				{
					int retVal = UnixSignal.WaitAny(signals, 100);

					if (retVal != CHECK_INTERVAL)
					{
						Log.InfoFormat("Received signal {0}, exiting", signals[retVal].Signum);
						break;
					}
				}
			}
		}

		private static void NonPosixWait(OnionGenerator[] generators)
		{
			while (AreGeneratorsRunning(generators))
			{
				Thread.Sleep(100);
			}
		}

		private static bool AreGeneratorsRunning(OnionGenerator[] generators)
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

		private static void TerminateAllGenerators(OnionGenerator[] generators)
		{
			Log.Info("Stopping onion address generation");
			if (generators != null)
			{
				Log.Debug("Sending stop command to workers");
				foreach (OnionGenerator o in generators)
				{
					if (o != null)
					{
						o.Stop();
					}
				}
				Log.Debug("Sent stop command to workers");

				Log.Debug("Waiting for workers to stop");
				while (AreGeneratorsRunning(generators))
				{
					Thread.Sleep(0);
				}
				Log.Debug("Workers stopped");
			}
			Log.Debug("Stopped onion address generation");
		}

		private static string PickOutputDirectory(OnionAddress onion)
		{
			string onionDir = onion.Onion;
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

		private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Fatal("Unhandled Exception", e.ExceptionObject as Exception);
		}
	}
}
