using System;
using System.ComponentModel;

namespace Xpdm.PurpleOnion
{
	class OnionGenerator
	{
		private readonly BackgroundWorker worker = new BackgroundWorker();
		
		public OnionGenerator()
		{
			worker.DoWork += GenerateOnion;
			worker.RunWorkerCompleted += RunWorkerCompleted;
		}
		
		public event EventHandler<OnionGeneratedEventArgs> OnionGenerated;
		
		public void StartGenerating()
		{
			worker.RunWorkerAsync();
		}
		
		protected virtual void GenerateOnion(object sender, DoWorkEventArgs e)
		{
			e.Result = OnionAddress.Create();
		}
		
		protected virtual void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			OnionGeneratedEventArgs args = new OnionGeneratedEventArgs {
				Result = (OnionAddress) e.Result,
			};
			
			OnOnionGenerated(args);

			if (!args.Cancel)
			{
				worker.RunWorkerAsync();
			}
			
		}
		
		protected virtual void OnOnionGenerated(OnionGeneratedEventArgs e)
		{
			EventHandler<OnionGeneratedEventArgs> handler = OnionGenerated;
			if (handler != null)
			{
				handler(this, e);
			}
		}
		
		public class OnionGeneratedEventArgs : EventArgs {
			new internal static readonly OnionGeneratedEventArgs Empty = new OnionGeneratedEventArgs();
			
			public bool Cancel { get; set; }
			public OnionAddress Result { get; set; }
		}
	}
}
