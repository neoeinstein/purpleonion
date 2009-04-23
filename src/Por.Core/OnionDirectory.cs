using System;
using System.IO;
using log4net;

namespace Por.Core
{
	public static class OnionDirectory
	{
		private static readonly ILog Log 
			= LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void WriteDirectory(OnionAddress onion, string dir)
		{
			if (onion == null)
			{
				throw new ArgumentNullException("onion");
			}
			if (string.IsNullOrEmpty(dir))
			{
				throw new ArgumentNullException("dir");
			}

			Log.Debug("Attempting to write onion directory");
			Directory.CreateDirectory(dir);
			onion.WriteToOnionFiles(dir);
		}

		public static OnionAddress ReadDirectory(string dir)
		{
			Log.Debug("Attempting to read onion directory");

			return InternalReadAndValidateDirectory(dir, false, false);
		}

		public static void Validate(string dir)
		{
			Validate(dir, true);
		}

		public static bool Validate(string dir, bool throwException)
		{
			Log.Debug("Attempting to validate onion directory");

			return InternalReadAndValidateDirectory(dir, throwException, true) != null;
		}

		private static OnionAddress InternalReadAndValidateDirectory(string dir, bool throwException, bool requireHostname)
		{
			Log.DebugFormat("Accessing onion directory: {0}", dir);
			if (!Directory.Exists(dir))
			{
				string message = "Onion directory '" + dir + "' does not exist.";
				if (throwException)
				{
					Log.Error(message);
					throw new DirectoryNotFoundException(message);
				}
				return null;
			}

			string onionKeyFile = Path.Combine(dir, OnionAddress.KeyFilename);
			string onionHostFile = Path.Combine(dir, OnionAddress.HostFilename);

			if (!File.Exists(onionKeyFile))
			{
				string message = "Onion private key file not found.";
				if (throwException)
				{
					Log.Error(message);
					throw new FileNotFoundException(message);
				}
				return null;
			}
			
			Log.Debug("Reading onion private key file");
			OnionAddress onion = OnionAddress.ReadFromOnionFile(onionKeyFile);

			if (!File.Exists(onionHostFile))
			{
				string message = "Onion hostname file not found.";
				if (requireHostname)
				{
					if (throwException)
					{
						Log.Error(message);
						throw new FileNotFoundException(message);
					}
					Log.Warn(message);
					return null;
				}
			}
			else
			{
				Log.Debug("Reading onion hostname file");
				string foundOnion = File.ReadAllText(onionHostFile)
					.Substring(0,OnionAddress.AddressLength);

				if (!onion.Onion.Equals(foundOnion))
				{
					string message = "Address in onion hostname file does not match onion address referred " +
						"to by the private key file. Expected: " + onion.Onion + " Found: " + foundOnion;
					
					if (requireHostname)
					{
						if (throwException)
						{
							Log.Error(message);
							throw new PorException(message);
						}
						Log.Warn(message);
						return null;
					}
				}
			}

			return onion;
		}
	}
}
