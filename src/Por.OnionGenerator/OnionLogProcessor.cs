using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Por.Core;
using log4net;

namespace Por.OnionGenerator
{
	sealed class OnionLogProcessor
	{
		private static readonly ILog Log 
			= LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string OnionLogFilename { get; protected set; }
		public Regex OnionPattern { get;  protected set; }
		
		public OnionLogProcessor(string onionLogFilename, Regex onionPattern)
		{
			if (string.IsNullOrEmpty(onionLogFilename))
			{
				throw new ArgumentNullException("onionLogFilename");
			}
			if (onionPattern == null)
			{
				throw new ArgumentNullException("onionPattern");
			}
			OnionLogFilename = onionLogFilename;
			OnionPattern = onionPattern;
		}

		public delegate string DirectoryPicker(OnionAddress onion);
		public DirectoryPicker PickDirectory { get; set; }
		public long MatchedCount { get; set; }
		public long MatchMax { get; set; }

		public void ProcessLog()
		{
			using (StreamReader file = File.OpenText(OnionLogFilename))
			{
				while (!file.EndOfStream && MatchedCount < MatchMax)
				{
					string line = file.ReadLine().Trim();
					if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
					{
						continue;
					}
					
					string[] record = line.Split(',');

					if (record.Length != 2)
					{
						Log.Warn("Invalid record does not contain two fields");
						continue;
					}

					string logOnion = record[0];
					if (OnionPattern.IsMatch(logOnion))
					{
						string pkiXml = record[1];

						try
						{
							using (OnionAddress onion = OnionAddress.FromXmlString(pkiXml))
							{
								if (OnionPattern.IsMatch(onion.Onion))
								{
									Log.InfoFormat("Found matching onion: {0}", onion.Onion);
									if (onion.IsPublicOnly)
									{
										Log.Warn("Unable to write matched address; record only " + 
										         "contains public portion of key");
									}
									string outputDir = PickDirectory(onion);
									if (outputDir != null)
									{
										OnionDirectory.WriteDirectory(onion, outputDir);
									}
									++MatchedCount;
								}
							}
						}
						catch (CryptographicException)
						{
							Log.Warn("Unable to parse key");
						}
					}
				}
			}

		}
	}
}
