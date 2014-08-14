using System;

namespace ArcGISRuntime.Toolkit.WindowsSampleViewer.Models
{
	public class Sample
	{
		public Sample()
		{
			NeedsOnlineConnection = true;
		}

		/// <summary>
		/// Gets or sets type of the Sample that is shown.
		/// </summary>
		public Type SampleType { get; set; }

		/// <summary>
		/// Gets or sets name of the sample.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets samples main category.
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Gets or sets samples sub category.
		/// </summary>
		public string Subcategory { get; set; }

		/// <summary>
		/// Gets or sets samples description.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets samples file name.
		/// </summary>
		public string SampleFile { get; set; }

		/// <summary>
		/// Gets or sets if sample needs online connection. Default is true.
		/// </summary>
		public bool NeedsOnlineConnection { get; set; }

		/// <summary>
		/// Gets or sets if sample needs offline data.
		/// </summary>
		public bool NeedsOfflineData { get; set; }
	}
}
