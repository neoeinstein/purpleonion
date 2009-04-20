using System;
using System.ComponentModel;

namespace Xpdm.PurpleOnion
{
	class OnionGenerator
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

		protected virtual void GenerateOnion(object sender, DoWorkEventArgs e)
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

		protected virtual void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
