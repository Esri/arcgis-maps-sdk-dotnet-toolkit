using ArcGISRuntime.Toolkit.DesktopSampleViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;

namespace ArcGISRuntime.Toolkit.DesktopSampleViewer
{
	public class SamplesDataSource
	{
		private const string SAMPLE_ASSEMBLY_NAME = "ArcGISRuntime.Toolkit.Samples.Desktop";
		private const string SAMPLE_DESCRIPTION_NAME = "ArcGISRuntime.Toolkit.Samples.Desktop.SampleDescriptions.xml";

		private static SamplesDataSource _current;

		/// <summary>
		/// Gets current instance of the sample datasource.
		/// </summary>
		public static SamplesDataSource Current
		{
			get
			{
				if (_current == null)
					_current = new SamplesDataSource();
				return _current;
			}
		}

		/// <summary>
		/// Gets the samples.
		/// </summary>
		public IEnumerable<Sample> Samples { get; private set; }

		/// <summary>
		/// Creates samples datasource.
		/// </summary>
		private SamplesDataSource()
		{
			// Load assembly
			var samplesAssembly = Assembly.Load(SAMPLE_ASSEMBLY_NAME);
			
			// Get all samples from the assembly
			var sampleTypes = from t in samplesAssembly.ExportedTypes
						where t.GetTypeInfo().IsSubclassOf(typeof(UserControl)) && t.FullName.Contains(".Samples.")
						select t;

			// Create samples from the found types
			Samples = (from sampleType in sampleTypes
						select new Sample()
					   {
						   SampleType = sampleType,
						   Name = SplitCamelCasedWords(sampleType.Name),
						   SampleFile = sampleType.Name,
						   Category = "Misc"
					   }).ToArray();

			//Update descriptions and category based on included XML Doc
			XDocument sampleDescriptions = null;
			try
			{
				sampleDescriptions = XDocument.Load(new StreamReader(
					samplesAssembly.GetManifestResourceStream(SAMPLE_DESCRIPTION_NAME)));
 
				foreach (XElement member in sampleDescriptions.Descendants("member"))
				{
					try
					{
						string name = (string)member.Attribute("name");
						if (name == null)
							continue;

						bool isType = name.StartsWith("T:", StringComparison.OrdinalIgnoreCase);
						if (isType)
						{
							// Find sample from Samples
							var match = (from sample in Samples 
											where name == "T:" + sample.SampleType.FullName 
											select sample).FirstOrDefault();
							
							// If sample was found, set values
							if (match != null)
							{
								var title = member.Descendants("title").FirstOrDefault();
								if (title != null && !string.IsNullOrWhiteSpace(title.Value))
									match.Name = title.Value.Trim();

								var summary = member.Descendants("summary").FirstOrDefault();
								if (summary != null)
									match.Description = summary.Value.Trim().Replace("<br/>", "\n");
								
								var category = member.Descendants("category").FirstOrDefault();
								if (category != null)
									match.Category = category.Value.Trim();
								
								var subcategory = member.Descendants("subcategory").FirstOrDefault();
								if (subcategory != null)
									match.Subcategory = subcategory.Value.Trim();
								
								var usesOnline = member.Descendants("usesonline").FirstOrDefault();
								if (usesOnline != null)
									match.NeedsOnlineConnection = bool.Parse(usesOnline.Value.Trim());

								var usesOffline = member.Descendants("usesoffline").FirstOrDefault();
								if (usesOffline != null)
									match.NeedsOfflineData = bool.Parse(usesOffline.Value.Trim());
							}
						}
					}
					catch { } //ignore
				}
			}
			catch { } //ignore
		}
		
		public List<SampleGroup> SamplesByCategory
		{
			get
			{
				List<SampleGroup> groups = new List<SampleGroup>();
				
				List<string> groupOrder = new List<string>
				{ 
					"Mapping", 
					"Layers", 
					"Geometry", 
					"Symbology", 
					"Tasks", 
					"Offline", 
					"Printing", 
					"Portal", 
					"Security", 
					"Extras", 
					"Toolkit"
				};
				
				// find all groups defined in the samples and put groups into correct order
				var query = (from item in Samples
							 orderby item.Category
							 group item by item.Category into g
							 select new { GroupName = g.Key, Items = g, GroupIndex = groupOrder.IndexOf(g.Key) })
							 .OrderBy(g => g.GroupIndex < 0 ? int.MaxValue : g.GroupIndex);

				// Create SampleGroups that were found and add those to the groups
				foreach (var g in query)
				{
					groups.Add(new SampleGroup(g.Items.OrderBy(i => i.Subcategory).ThenBy(i => i.Name)) 
					{ 
						Name = g.GroupName 
					});
				}

				//// Define order of Mapping samples
				//SampleGroup mappingSamplesGroup = groups.Where(i => i.Name == "Mapping").First();
				//List<Sample> mappingSamples = new List<Sample>();

				////Add any missing samples
				//foreach (var item in mappingSamplesGroup.Items)
				//	if (!mappingSamples.Contains(item))
				//		mappingSamples.Add(item);

				//SampleGroup newMappingSamplesGroup = new SampleGroup(mappingSamples) { Name = mappingSamplesGroup.Name };
				//groups[groups.FindIndex(g => g.Name == mappingSamplesGroup.Name)] = newMappingSamplesGroup;

				return groups;
			}
		}

		/// <summary>
		/// Split camel cased file names to sample names.
		/// </summary>
		private static string SplitCamelCasedWords(string value)
		{
			var text = System.Text.RegularExpressions.Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
			return text.Replace("Arc GIS", "ArcGIS ");
		}
	}
}
