using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace CreateMooToolsReferences
{
	class Program
	{
		static void Main(string[] args)
		{
			string path = Application.StartupPath + Path.DirectorySeparatorChar;
			string json = path + "scripts.json";
			string rf = path + @"References\";
			string mooSource = @"../../JavaScript/MooTools/";
			string refSource = @"../";

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			var obj = serializer.Deserialize<Dictionary<string, Dictionary<string, JavaScriptFile>>>(File.ReadAllText(json));

			var deps = new Dictionary<string, string>();

			foreach (var directory in obj)
			{
				Console.WriteLine(directory.Key);
				if (!Directory.Exists(rf + directory.Key)) Directory.CreateDirectory(rf + directory.Key);
				foreach (var file in directory.Value)
				{
					Console.WriteLine("\t" + file.Key);
					string f = rf + directory.Key + Path.DirectorySeparatorChar + file.Key + ".js";
					deps.Add(file.Key, String.Format("//@reference({0}{1}/{2}.js)", refSource, directory.Key, file.Key));
					//deps.Add(file.Key, String.Format("//@reference(MooTools.{0}.{1}.js)", directory.Key, file.Key));
					if (!File.Exists(f))
					{
						TextWriter w = new StreamWriter(f);
						var item = file.Value;
						foreach (string i in item.Deps)
							if(file.Key != i) w.WriteLine(deps[i]);

						w.WriteLine("//@include({0}{1}/{2}.js)", mooSource, directory.Key, file.Key);
						//w.WriteLine("//@include(Calyptus.ClientSide.JavaScript.MooTools.{0}.{1}.js)", directory.Key, file.Key);
						w.Close();
					}
				}
			}
			Console.ReadLine();
		}
	}
}
