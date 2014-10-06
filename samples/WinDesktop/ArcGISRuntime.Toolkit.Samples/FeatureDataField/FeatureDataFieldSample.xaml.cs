using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Query;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ArcGISRuntime.Toolkit.Samples.Desktop.FeatureDataField
{
	/// <summary>
	/// Demonstrates how to show attributes using FeatureDataField.
	/// </summary>
	/// <title>Feature data field</title>
	/// <category>Toolkit</category>
	/// <subcategory>FeatureDataField</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public partial class FeatureDataFieldSample
	{
		public FeatureDataFieldSample()
		{
			InitializeComponent();
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
					WhereClause = "incidentid <> ''" , MaximumRows = 100
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
				MessageBox.Show(string.Format("Error occured : {0}", exception.ToString()), "Sample error");
			}
		}

		/// <summary>
		/// Model class to wrap symbol and feature information into one object for binding
		/// </summary>
		internal class Assessment
		{
			public Symbol Symbol {get; set;}
			public GeodatabaseFeature Feature { get; set; }
		}
	}
}
