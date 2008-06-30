using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Calyptus.ResourceManager
{
	internal class SyntaxReader
	{
		private Regex _parser;

		private IResourceLocation _baseLocation;
		private string _ext;

		public SyntaxReader(TextReader textReader, bool parseSingleLineComments, IResourceLocation baseLocation, string defaultExtension)
		{
			_baseLocation = baseLocation;
			_ext = defaultExtension;

			_parser = new Regex("(?:^|\\s)\\@(include|import|build|compress)[\\(\\s]\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)\\,]+)[\\'\\\"]?\\s*(?:,\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)\\,]+)[\\'\\\"]?)?\\s*(?:,\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)\\,]+)[\\'\\\"]?)?\\s*[\\)\\n\\;]", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

			_includes = new List<IResourceLocation>();
			_references = new List<IResourceLocation>();
			_builds = new List<IResourceLocation>();

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
					if (!isInComment && lastc == '/' && (c == '*' || (c == '/' && parseSingleLineComments)))
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
						HasContent = true;
						textReader.Close();
						return;
					}
					lastc = c;
				}
			textReader.Close();
			if (isInComment)
			{
				ParseBlock(block.ToString());
			}
		}

		private void ParseBlock(string block)
		{
			foreach (Match m in _parser.Matches(block))
			{
				string command = m.Groups[1].Value;
				string param1 = m.Groups[2].Success ? m.Groups[2].Value : null;
				string param2 = m.Groups[3].Success ? m.Groups[3].Value : null;
				string param3 = m.Groups[4].Success ? m.Groups[4].Value : null;

				if (command.Equals("compress", StringComparison.OrdinalIgnoreCase))
				{
					Compress = param1;
					continue;
				}

				List<IResourceLocation> l =
					command.Equals("include", StringComparison.OrdinalIgnoreCase) ? _includes : (
						command.Equals("build", StringComparison.OrdinalIgnoreCase) ? _builds : (
							command.Equals("import", StringComparison.OrdinalIgnoreCase) ? _references : null
						)
					);

				if (l != null)
				{
					IEnumerable<IResourceLocation> ls = ResourceLocations.GetLocations(_baseLocation, param2 != null ? param1 : null, param2 ?? param1);
					if (ls == null && param2 == null && _ext != null && !param1.EndsWith("*") && !param1.Equals(_ext, StringComparison.OrdinalIgnoreCase))
						ls = ResourceLocations.GetLocations(_baseLocation, null, param1 + _ext);
					if (ls != null)
						l.AddRange(ls);
				}
			}
		}

		private List<IResourceLocation> _includes;
		public IEnumerable<IResourceLocation> Includes
		{
			get
			{
				return _includes.Count == 0 ? null : _includes;
			}
		}

		private List<IResourceLocation> _builds;
		public IEnumerable<IResourceLocation> Builds
		{
			get
			{
				return _builds.Count == 0 ? null : _builds;
			}
		}

		private List<IResourceLocation> _references;
		public IEnumerable<IResourceLocation> References
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

		public bool HasContent { get; private set; }
	}
}
