using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Query;
using System;
using System.Collections.Generic;
using System.Windows;
using Windows.UI.Popups;
using Windows.UI.Xaml.Data;

namespace ArcGISRuntime.Toolkit.Samples.Windows.FeatureDataField
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
					WhereClause = "incidentid <> ''" , 
					MaximumRows = 100
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
			public Symbol Symbol {get; set;}
			public GeodatabaseFeature Feature { get; set; }
		}
	}

	/// <summary>
	/// DatePicker uses DateTimeOffset type in Date property insetead of DateTime so we need to 
	/// convert DateTime used by FeatureDataField to DateTimeOffset and back.
	/// </summary>
	public class DateTimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			try
			{
				DateTime date = (DateTime)value;
				return new DateTimeOffset(date);
			}
			catch (Exception ex)
			{
				return DateTimeOffset.MinValue;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				DateTimeOffset dto = (DateTimeOffset)value;
				return dto.DateTime;
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}
}
