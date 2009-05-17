using System;
using Microsoft.Build.Utilities;
using System.IO;
using Microsoft.Build.Framework;
using System.Reflection;

namespace Calyptus.ResourceManager.MSBuild
{
	public class PackageWebResources : Task
	{
		[Required]
		public ITaskItem[] ResourceFiles { get; set; }
		[Required]
		public string OutputPath { get; set; }

		public ITaskItem[] References { get; set; }

		public string Language { get; set; }
		public string LanguageSourceExtension { get; set; }
		
		[Output]
		public ITaskItem[] AdditionalReferences
		{
			get
			{
				return null;
			}
		}

		[Output]
		public ITaskItem[] EmbeddedFiles
		{
			get
			{
				return null;
			}
		}

		[Output]
		public ITaskItem[] SourceFiles
		{
			get
			{
				return null;
			}
		}

		[Output]
		public ITaskItem[] GeneratedFiles
		{
			get
			{
				return null;
			}
		}

		public override bool Execute()
		{
			foreach (var i in ResourceFiles)
			{
				this.Log.LogMessage("ResourceFiles: " + i.ItemSpec);
				foreach (string n in i.MetadataNames)
					this.Log.LogMessage(" - " + n + " = " + i.GetMetadata(n));
			}

			foreach (var i in References)
			{
				this.Log.LogMessage("References: " + i.ItemSpec);
				foreach (string n in i.MetadataNames)
					this.Log.LogMessage(" - " + n + " = " + i.GetMetadata(n));
			}

			this.Log.LogMessage("Language: " + Language);
			this.Log.LogMessage("LanguageSourceExtension: " + LanguageSourceExtension);

			this.Log.LogError("OutputPath: " + OutputPath + " Fullpath:" + Path.GetFullPath(OutputPath));
			return false;
		}
	}
}