using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ArcGISRuntime.Toolkit.WindowsSampleViewer
{
	public class AppState : INotifyPropertyChanged
	{
		private static AppState m_Current;
		public static AppState Current
		{
			get
			{
				if (m_Current == null)
				{
					m_Current = new AppState();
					m_Current.SetDefaultTitle();
				}
				return m_Current;
			}
		}
		public void SetDefaultTitle()
		{
			CurrentSampleTitle = "ArcGIS Runtime SDK for .NET - Samples";
		}
		private string m_CurrentSampleTitle;

		public string CurrentSampleTitle
		{
			get { return m_CurrentSampleTitle; }
			set { m_CurrentSampleTitle = value; OnPropertyChanged(); }
		}

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		private bool m_CanGoBack;

		public bool CanGoBack
		{
			get { return m_CanGoBack; }
			set { m_CanGoBack = value; OnPropertyChanged(); }
		}
	}
}