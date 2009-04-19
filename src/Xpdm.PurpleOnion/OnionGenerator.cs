using System;
using System.ComponentModel;

namespace Xpdm.PurpleOnion
{
	class OnionGenerator
	{
		private readonly BackgroundWorker worker = new BackgroundWorker();
		public bool Running { get; protected set; }
		
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
			worker.CancelAsync();
		}
		
		protected virtual void GenerateOnion(object sender, DoWorkEventArgs e)
		{
			if (worker.CancellationPending)
			{
				e.Cancel = true;
				return;
			}

			e.Result = OnionAddress.Create();

			if (worker.CancellationPending)
			{
				e.Cancel = true;
			}
		}

		protected virtual void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled)
			{
				Running = false;
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
