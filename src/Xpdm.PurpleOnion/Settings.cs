using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Mono.Options;

namespace Xpdm.PurpleOnion
{
	class Settings
	{
		public Regex ToMatch { get; set; }
		public string OutFilename { get; set; }
		public string InFilename { get; set; }
		public string BaseDir { get; set; }
		public List<string> ExtraArgs { get; set; }
		public int Verbosity { get; set; }
		public bool ShouldShowHelp { get; set; }
		
		OptionSet options;
		
		public Settings()
		{
			options = new OptionSet() {
				{ "m|match=", "create onion directories for matches",
					v => { if (v != null) ToMatch = new Regex(v, RegexOptions.Compiled); } },
				{ "o|out=", "file to which generated pairs should be written, exclusive of -i",
					v => { if (v != null) OutFilename = v; } },
				{ "i|in=", "read in file from a previous run, exclusive of -o, requires -m",
					v => { if (v != null) InFilename = v; } },
				{ "b|basedir=", "base working directory",
					v => { if (v != null) BaseDir = v; } },
				{ "v", "increase output verbosity",
					v => { if (v != null) ++Verbosity; } },
				{ "h|help",  "show this message and exit", 
					v => ShouldShowHelp = v != null }
			};
			BaseDir = ".";
		}
		
		public bool TryParse(string[] args)
		{
			try
			{
			}
			catch (OptionException e)
			{
				string appName = Environment.GetCommandLineArgs()[0];
				Console.Error.Write(appName + ": ");
				Console.Error.WriteLine(e.Message);
				Console.Error.WriteLine("Try `" + appName + " --help' for more information.");
				return false;
			}
			return true;
		}
		
		public void ShowHelp(TextWriter o)
		{
			string appName = Environment.GetCommandLineArgs()[0];
			o.WriteLine("Usage: " + appName + " [-v] [-b] [-i filename|-o filename] [-m regex]");
			o.WriteLine("Brute-forces the creation of many RSA key-pairs attempting to find one whose");
			o.WriteLine("Tor onion address matches a given pattern. For the creation of vanity");
			o.WriteLine("onion addresses or burning through excess entropy.");
			o.WriteLine();
			o.WriteLine("Options:");
			options.WriteOptionDescriptions(o);
		}
	}
}
