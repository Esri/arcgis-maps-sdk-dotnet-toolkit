using System.Collections.Generic;

namespace ArcGISRuntime.Toolkit.PhoneSampleViewer.Models
{
	/// <summary>
	/// Represents a group of samples
	/// </summary>
	public class SampleGroup
	{
		public SampleGroup(IEnumerable<Sample> samples)
		{
			Items = samples;
		}

		/// <summary>
		/// Gets or sets name of the group.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or samples from the group.
		/// </summary>
		public IEnumerable<Sample> Items { get; private set; }
	}
}
