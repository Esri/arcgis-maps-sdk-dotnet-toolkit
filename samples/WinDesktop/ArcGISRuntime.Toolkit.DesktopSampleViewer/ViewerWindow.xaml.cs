using ArcGISRuntime.Toolkit.DesktopSampleViewer.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ArcGISRuntime.Toolkit.DesktopSampleViewer
{
	public partial class ViewerWindow : Window
	{
		public ViewerWindow()
		{
			InitializeComponent();
			LoadSamples();
		}

		private void LoadSamples()
		{
			var samples = SamplesDataSource.Current.SamplesByCategory;
		
			foreach (var group in samples)
			{
				MenuItem samplesItem = new MenuItem() { Header = group.Name };
				var subGroups = group.Items.GroupBy(g => g.Subcategory);
				foreach (var subGroup in subGroups.Where(sg => sg.Key != null))
				{
					MenuItem subGroupItem = new MenuItem() { Header = subGroup.Key };
					foreach (var sample in subGroup)
					{
						CreateSampleMenuItem(subGroupItem, sample);
					}
					samplesItem.Items.Add(subGroupItem);
				}
				foreach (var sample in group.Items.Where(g => g.Subcategory == null))
				{
					CreateSampleMenuItem(samplesItem, sample);
				}
				menu.Items.Add(samplesItem);
			}
		}

		private void CreateSampleMenuItem(MenuItem parentMenu, Sample sample)
		{
			MenuItem sampleitem = new MenuItem()
			{
				Header = sample.Name,
				ToolTip = new TextBlock() { Text = sample.Description, MaxWidth = 300, TextWrapping = TextWrapping.Wrap }
			};
			parentMenu.Items.Add(sampleitem);
			sampleitem.Click += (s, e) => { sampleitem_Click(sample, s as MenuItem); };
		}

		MenuItem currentSampleMenuItem;
		private void sampleitem_Click(Sample sample, MenuItem menu)
		{
			var c = sample.SampleType.GetConstructor(new Type[] { });
			var ctrl = c.Invoke(new object[] { }) as UIElement;
			
			sampleContainer.Child = ctrl;
			
			if (currentSampleMenuItem != null)
				currentSampleMenuItem.IsChecked = false;
			menu.IsChecked = true;
			
			currentSampleMenuItem = menu;
			StatusBar.DataContext = sample;
		}
	}
}