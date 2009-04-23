using System;
using System.ComponentModel;
using Por.Core;

namespace Por.OnionGenerator
{
	sealed class OnionGenerator
	{
		private readonly BackgroundWorker worker = new BackgroundWorker();
		public bool Running { get; protected set; }
		public bool StopRequested { get; protected set; }
		
		public OnionGenerator()
		{
			worker.DoWork += GenerateOnion;
			worker.RunWorkerCompleted += RunWorkerCompleted;
		}
		
		public event EventHandler<OnionGeneratedEventArgs> OnionGenerated;
		
		public void Start()
		{
			Running = true;
			worker.RunWorkerAsync();
		}

		public void Stop()
		{
			StopRequested = true;
		}

		private void GenerateOnion(object sender, DoWorkEventArgs e)
		{
			if (StopRequested)
			{
				e.Cancel = true;
				return;
			}

			OnionAddress onion = OnionAddress.Create();
			e.Result = onion;

			if (StopRequested)
			{
				e.Cancel = true;
			}
		}

		private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled || StopRequested)
			{
				Running = false;
				StopRequested = false;
				return;
			}

			OnionGeneratedEventArgs args = new OnionGeneratedEventArgs {
				Result = (OnionAddress) e.Result,
			};
			
			OnOnionGenerated(args);

			if (!args.Cancel)
			{
				worker.RunWorkerAsync();
			}
			else
			{
				Running = false;
				StopRequested = false;
			}
		}
		
		private void OnOnionGenerated(OnionGeneratedEventArgs e)
		{
			EventHandler<OnionGeneratedEventArgs> handler = OnionGenerated;
			if (handler != null)
			{
				handler(this, e);
			}
		}
		
		public sealed class OnionGeneratedEventArgs : EventArgs {
			public bool Cancel { get; set; }
			public OnionAddress Result { get; set; }
		}
	}
}
