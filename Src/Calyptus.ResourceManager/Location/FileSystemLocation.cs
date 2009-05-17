using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace Calyptus.ResourceManager
{
	public class FileSystemLocation : FileLocation
	{
		private FileSystemWatcher fileSystemWatcher;
		private AssemblyName[] referencedAssemblies;

		public FileSystemLocation(string absolutePath)
		{
			FullName = Path.GetFullPath(absolutePath);
			VerifyPath();
		}

		public FileSystemLocation(string basePath, string relativePath)
		{
			FullName = Path.GetFullPath(Path.Combine(basePath, relativePath));
			VerifyPath();
		}

		public FileSystemLocation(string absolutePath, AssemblyName[] referencedAssemblies)
		{
			FullName = Path.GetFullPath(absolutePath);
			VerifyPath();
			this.referencedAssemblies = referencedAssemblies;
		}

		~FileSystemLocation()
		{
			if (fileSystemWatcher != null)
				fileSystemWatcher.Dispose();
		}

		private void VerifyPath()
		{
			if (!File.Exists(FullName)) throw new ArgumentException("The file or directory '" + FullName + "' doesn't exist.");
		}

		public string FullName
		{
			get;
			private set;
		}

		public override Stream GetStream()
		{
			return new FileStream(FullName, FileMode.Open, FileAccess.Read, FileShare.Read); 
		}

		public override string FileName
		{
			get { return System.Web.VirtualPathUtility.GetFileName(FullName); }
		}

		public override bool Equals(object obj)
		{
			VirtualPathLocation l = obj as VirtualPathLocation;
			if (l == null) return false;
			return l.VirtualPath.Equals(FullName, StringComparison.InvariantCultureIgnoreCase);
		}

		public override int GetHashCode()
		{
			return FullName.ToLower().GetHashCode();
		}

		public override string ToString()
		{
			return FullName;
		}

		public override void MonitorChanges(Action onChange)
		{
			if (this.fileSystemWatcher == null) 
			{
				this.fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(this.FullName), Path.GetFileName(this.FullName));

				FileSystemEventHandler handler = (sender, e) => {
					this.OnChanged();
					this.fileSystemWatcher.Dispose();
				};

				this.fileSystemWatcher.Deleted += handler;
				this.fileSystemWatcher.Changed += handler;
				this.fileSystemWatcher.Renamed += (sender, e) =>
				{
					this.OnChanged();
					this.fileSystemWatcher.Dispose();
				};
			}
			base.MonitorChanges(onChange);
		}

		public override IResourceLocation GetRelativeLocation(string name)
		{
			string path = Path.Combine(Path.GetDirectoryName(this.FullName), name);
			if (File.Exists(path))
				return new FileSystemLocation(path, this.referencedAssemblies);
			if (this.referencedAssemblies != null)
				foreach (AssemblyName r in this.referencedAssemblies)
				{
					IResourceLocation l = ResourceLocations.GetAssemblyLocation(Assembly.Load(r), name);
					if (l != null) return l;
				}
			return null;
		}

		public override IEnumerable<IResourceLocation> GetRelativeLocations(string name)
		{
			List<IResourceLocation> ls = new List<IResourceLocation>();
			int i = name.LastIndexOf(System.IO.Path.PathSeparator);
			if (i < 0) i = 0;
			if (name.IndexOf('*', i) < 0)
			{
				IResourceLocation l = GetRelativeLocation(name);
				if (l != null) return new IResourceLocation[] { l };
			}
			else
			{
				string path = Path.Combine(Path.GetDirectoryName(this.FullName), name.Substring(0, i)),
					filename = name.Substring(i);
				path = Path.GetFullPath(path);
				foreach (FileInfo file in new DirectoryInfo(path).GetFiles(filename, SearchOption.TopDirectoryOnly))
					ls.Add(new FileSystemLocation(file.FullName, this.referencedAssemblies));
			}
			if (this.referencedAssemblies != null)
				foreach (AssemblyName r in this.referencedAssemblies)
				{
					IEnumerable<IResourceLocation> l = ResourceLocations.GetAssemblyLocations(Assembly.Load(r), name);
					if (l != null)
						ls.AddRange(l);
				}
			return (ls.Count > 0) ? ls : null;
		}
	}
}
