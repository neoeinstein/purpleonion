using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Por.Core;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;

namespace Por.OnionGenerator
{
	sealed class OnionGenerator
	{
		private static readonly ILog Log 
			= LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly ILoggerRepository OnionRepository;
		private readonly ILog OnionLog;
		private readonly ForwardingAppender OnionAppender;

		private readonly BackgroundWorker worker = new BackgroundWorker();
		public Regex OnionPattern { get; set; }
		public bool Running { get; protected set; }
		public bool StopRequested { get; protected set; }
		
		public long GeneratedCount { get; protected set; }
		public long GenerateMax { get; set; }

		public long MatchedCount { get; protected set; }
		public long MatchMax { get; set; }

		public void ResetCount()
		{
			GeneratedCount = 0;
			MatchedCount = 0;
		}

		public string OnionOutputFile
		{
			set
			{
				OnionAppender.RemoveAllAppenders();
				if(!string.IsNullOrEmpty(value))
				{
					FileAppender appender = new FileAppender {
						Layout = new PatternLayout("%m%n"),
						Encoding = System.Text.Encoding.ASCII,
						AppendToFile = true,
						File = value,
					};
					appender.ActivateOptions();
					OnionAppender.AddAppender(appender);
				}
			}
		}
		
		public OnionGenerator()
		{
			string name = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
			Guid guid = Guid.NewGuid();
			OnionRepository = LogManager.CreateRepository(name + guid.ToString());
			OnionLog = LogManager.GetLogger(OnionRepository.Name, name);
			OnionAppender = new ForwardingAppender();
			BasicConfigurator.Configure(OnionRepository, OnionAppender);

			worker.DoWork += GenerateOnionsLoop;
			worker.RunWorkerCompleted += Stopped;

			GenerateMax = MatchMax = long.MaxValue;
		}
		
		public void Start()
		{
			Running = true;
			worker.RunWorkerAsync();
		}

		public void Stop()
		{
			StopRequested = true;
		}

		private void GenerateOnionsLoop(object sender, DoWorkEventArgs e)
		{
			Log.Info("Beginning onion address generation");
			while (!StopRequested && GeneratedCount <= GenerateMax && MatchedCount <= MatchMax)
			{
				Log.Debug("Generating onion");
				using(OnionAddress onion = OnionAddress.Create())
				{
					Log.DebugFormat("Onion generated: {0}", onion.Onion);
		
					if (StopRequested) break;
	
					OnionLog.InfoFormat("{0},{1}", onion.Onion, onion.ToXmlString(true));
	
					if (StopRequested) break;
	
					if(OnionPattern != null && OnionPattern.IsMatch(onion.Onion))
					{
						Log.InfoFormat("Found matching onion: {0}", onion.Onion);
						string outputDir = PickDirectory(onion);
						if (outputDir != null)
						{
							OnionDirectory.WriteDirectory(onion, outputDir);
						}
						++MatchedCount;
					}

					++GeneratedCount;
				}
			}
			Log.Debug("Stopped onion address generation");

			if (StopRequested)
			{
				e.Cancel = true;
			}
		}

		public delegate string DirectoryPicker(OnionAddress onion);
		public DirectoryPicker PickDirectory { get; set; }

		private void Stopped(object sender, RunWorkerCompletedEventArgs e)
		{
			Running = false;
			StopRequested = false;
		}
	}
}
