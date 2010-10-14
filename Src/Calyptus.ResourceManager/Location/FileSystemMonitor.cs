using System;
using System.Collections.Generic;
using System.IO;

namespace Calyptus.ResourceManager
{
	internal class FileSystemMonitor
	{
		private string filename;
		private FileSystemWatcher fileSystemWatcher;
		private List<Action> callbacks;
		private int currentExecutionIndex;

		public FileSystemMonitor(string filename)
		{
			this.filename = filename;
			this.callbacks = new List<Action>();
			currentExecutionIndex = -1;
		}

		public void Subscribe(Action callback)
		{
			if (fileSystemWatcher == null)
			{
				fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(filename), Path.GetFileName(filename));
				FileSystemEventHandler handler = (sender, e) => { this.Fire(); };
				fileSystemWatcher.Deleted += handler;
				fileSystemWatcher.Changed += handler;
				fileSystemWatcher.Renamed += (sender, e) => { this.Fire(); };
			}

			if (!callbacks.Contains(callback)) callbacks.Add(callback);
		}

		public void Unsubscribe(Action callback)
		{
			int i = callbacks.IndexOf(callback);
			callbacks.RemoveAt(i);
			if (i <= currentExecutionIndex) currentExecutionIndex--;
			if (callbacks.Count == 0 && fileSystemWatcher != null)
			{
				fileSystemWatcher.Dispose();
				fileSystemWatcher = null;
			}
		}

		public void Fire()
		{
			lock (this)
			{
				try
				{
					for (currentExecutionIndex = 0; currentExecutionIndex < callbacks.Count; currentExecutionIndex++)
						callbacks[currentExecutionIndex]();
				}
				finally
				{
					currentExecutionIndex = -1;
				}
			}
		}

		public bool HasCallbacks
		{
			get
			{
				return callbacks.Count > 0;
			}
		}
	}
}
