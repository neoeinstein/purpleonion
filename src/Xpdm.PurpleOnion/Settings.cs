using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Mono.Options;

namespace Xpdm.PurpleOnion
{
	sealed class Settings
	{
		public readonly string AppName = AppDomain.CurrentDomain.FriendlyName;
		public Regex ToMatch { get; set; }
		public string OutFilename { get; set; }
		public string InFilename { get; set; }
		public string BaseDir { get; set; }
		public int WorkerCount { get; set; }
		public List<string> ExtraArgs { get; set; }
		public int Verbosity { get; set; }
		public bool ShouldShowHelp { get; set; }
		
		OptionSet options;
		
		public Settings()
		{
			options = new OptionSet() {
				{ "m|match=", "create onion directories for matches",
					v => { if (v != null) ToMatch = new Regex(v, RegexOptions.Compiled); } },
				{ "o|out=", "file to which generated pairs should be written\nexclusive of -i",
					v => { if (v != null) OutFilename = v; } },
				{ "i|in=", "read in a file from a previous run\nexclusive of -o, requires -m",
					v => { if (v != null) InFilename = v; } },
				{ "b|basedir=", "base working directory",
					v => { if (v != null) BaseDir = v; } },
				{ "n|num=", "number of child workers to spawn\n(default: 2*num_proc)",
					(int v) => WorkerCount = v },
				{ "v", "increase output verbosity",
					v => { if (v != null) ++Verbosity; } },
				{ "h|help",  "show this message and exit", 
					v => ShouldShowHelp = v != null }
			};
			BaseDir = ".";
			WorkerCount = Environment.ProcessorCount * 2;
		}
		
		public bool TryParse(string[] args)
		{
			try
			{
				ExtraArgs = options.Parse(args);
			}
			catch (OptionException e)
			{
				Console.Error.Write(AppName + ": ");
				Console.Error.WriteLine(e.Message);
				Console.Error.WriteLine("Try `" + AppName + " --help' for more information.");
				return false;
			}
			return true;
		}
		
		public void ShowHelp(TextWriter o)
		{
			o.WriteLine("Usage: " + AppName + " [-v] [-b] [[-i|-o] filename] [-m regex] [-n number]");
			o.WriteLine("Brute-forces the creation of many RSA key-pairs attempting to find one whose");
			o.WriteLine("Tor onion address matches a given pattern. For the creation of vanity");
			o.WriteLine("onion addresses or burning through excess entropy.");
			o.WriteLine();
			o.WriteLine("Options:");
			options.WriteOptionDescriptions(o);
		}
	}
}
