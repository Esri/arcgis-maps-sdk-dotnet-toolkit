using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace ArcGISRuntime.Toolkit.Samples.Phone.SymbolDisplay
{
	/// <summary>
	/// Demonstrates how to show symbol outside of the map using SymbolDisplay control.
	/// </summary>
	/// <title>SymbolDisplay</title>
	/// <category>Toolkit</category>
	/// <subcategory>SymbolDisplay</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class SymbolDisplaySample : Page
	{
		public SymbolDisplaySample()
		{
			this.InitializeComponent();
			LoadFeatures();
		}

		private async void LoadFeatures()
		{
			try
			{
				var damageAssessmentTable = await ServiceFeatureTable.OpenAsync(
					new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0"));
				damageAssessmentTable.OutFields = OutFields.All;

				// Get incidents that has incidentid
				var damageAssessments = await damageAssessmentTable.QueryAsync(new QueryFilter() 
				{
					WhereClause = "incidentid <> ''",
					MaximumRows = 10
				});

				var assessments = new List<Assessment>();
				// Create assessment items from search results
				foreach (var assessment in damageAssessments)
				{
					var feature = assessment as GeodatabaseFeature;

					assessments.Add(new Assessment
					{
						Feature = feature,
						Symbol = damageAssessmentTable.ServiceInfo.DrawingInfo.Renderer.GetSymbol(feature)
					});
				}

				FeatureList.ItemsSource = assessments;

			}
			catch (Exception exception)
			{
				var _x = new MessageDialog(string.Format("Error occured : {0}", exception.ToString()), "Sample error").ShowAsync();
			}
		}

		/// <summary>
		/// Model class to wrap symbol and feature information into one object for binding
		/// </summary>
		internal class Assessment
		{
			public Esri.ArcGISRuntime.Symbology.Symbol Symbol { get; set; }
			public GeodatabaseFeature Feature { get; set; }
		}
	}
}
