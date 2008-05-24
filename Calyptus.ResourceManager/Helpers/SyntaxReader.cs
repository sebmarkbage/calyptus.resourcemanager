using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Calyptus.ResourceManager
{
	public class SyntaxReader
	{
		private Regex _parser;

		public SyntaxReader(TextReader textReader)
		{
			//  (?:^|\s)\@(include|using|compress)\(\s*[\'\"]?((?<=\")[^\"]*(?=\")|(?<=\')[^\']*(?=\')|[^\s\']+)[\'\"]?\s*(?:,\s*[\'\"]?((?<=\")[^\"]*(?=\")|(?<=\')[^\']*(?=\')|[^\s\']+)[\'\"]?)?\s*(?:,\s*[\'\"]?((?<=\")[^\"]*(?=\")|(?<=\')[^\']*(?=\')|[^\s\']+)[\'\"]?)?\s*\)

			_parser = new Regex("(?:^|\\s)\\@(include|reference|build|compress)\\(\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)]+)[\\'\\\"]?\\s*(?:,\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)]+)[\\'\\\"]?)?\\s*(?:,\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)]+)[\\'\\\"]?)?\\s*\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

			_includes = new List<FileReference>();
			_references = new List<FileReference>();
			_builds = new List<FileReference>();

			bool isInComment = false;
			bool isMultiLineComment = false;
			StringBuilder block = new StringBuilder();
			int l;
			char[] buffer = new char[10];
			char lastc = (char)0x0;
			while ((l = textReader.ReadBlock(buffer, 0, buffer.Length)) > 0)
				for (int i = 0; i < l; i++)
				{
					char c = buffer[i];
					if (!isInComment && lastc == '/' && (c == '/' || c == '*'))
					{
						isInComment = true;
						isMultiLineComment = c == '*';
						while(i + 1 < l && buffer[i + 1] == (isMultiLineComment ? '*' : '/'))
							i++;
					}
					else if (isInComment && ((isMultiLineComment && lastc == '*' && c == '/') || (!isMultiLineComment && (c == '\n' || (lastc == '\r' && c == '\n')))))
					{
						isInComment = false;
						ParseBlock(block.ToString());
						block = new StringBuilder();
					}
					else if (isInComment)
						block.Append(c);
					else if (c != '/' && c != '\t' && c != ' ' && c != ' ' && c != '\r' && c != '\n')
					{
						textReader.Close();
						return;
					}
					lastc = c;
				}

			if (isInComment)
			{
				ParseBlock(block.ToString());
			}
		}

		private void ParseBlock(string block)
		{
			foreach (Match m in _parser.Matches(block))
			{
				string command = m.Groups[1].Value.ToLower();
				string param1 = m.Groups[2].Success ? m.Groups[2].Value : null;
				string param2 = m.Groups[3].Success ? m.Groups[3].Value : null;
				string param3 = m.Groups[4].Success ? m.Groups[4].Value : null;
				switch (command)
				{
					case "include":
						_includes.Add(
								param2 != null ? new FileReference { Assembly = param1, Filename = param2 } : new FileReference { Filename = param1 }
							);
						break;
					case "build":
						_builds.Add(
								param2 != null ? new FileReference { Assembly = param1, Filename = param2 } : new FileReference { Filename = param1 }
							);
						break;
					case "reference":
						_references.Add(
								param2 != null ? new FileReference { Assembly = param1, Filename = param2 } : new FileReference { Filename = param1 }
							);
						break;
					case "compress":
						Compress = param1;
						break;
				}
			}
		}

		private IList<FileReference> _includes;
		public IEnumerable<FileReference> Includes
		{
			get
			{
				return _includes.Count == 0 ? null : _includes;
			}
		}

		private IList<FileReference> _builds;
		public IEnumerable<FileReference> Builds
		{
			get
			{
				return _builds.Count == 0 ? null : _builds;
			}
		}

		private IList<FileReference> _references;
		public IEnumerable<FileReference> References
		{
			get
			{
				return _references.Count == 0 ? null : _references;
			}
		}

		public string Compress
		{
			get;
			private set;
		}

		public struct FileReference
		{
			public string Assembly;
			public string Filename;

			public IResourceLocation GetLocation(IResourceLocation baseLocation)
			{
				return LocationHelper.GetLocation(baseLocation, Assembly, Filename);
			}
		}
	}
}
