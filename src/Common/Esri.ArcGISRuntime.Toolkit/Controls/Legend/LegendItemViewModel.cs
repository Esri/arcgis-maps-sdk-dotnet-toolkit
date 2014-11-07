// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
#if NETFX_CORE
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
#else
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls.Primitives
{
	/// <summary>
	/// Represents a basic item in the legend/TOC.
	/// A basic item contains a label and an image. 
	/// </summary>
	public class LegendItemViewModel : INotifyPropertyChanged
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LegendItemViewModel"/> class.
		/// </summary>
		public LegendItemViewModel()
		{
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="LegendItemViewModel"/> class from a <see cref="LegendItemInfo"/>.
		/// </summary>
		/// <param name="legendItemInfo">The legend item info.</param>
		/// <param name="geometryType">Type of the geometry.</param>
		internal LegendItemViewModel(LegendItemInfo legendItemInfo, GeometryType geometryType)
			: this()
		{
			Label = legendItemInfo.Label;
			Symbol = legendItemInfo.Symbol;
			GeometryType = geometryType;
		}

		#endregion

		#region Label
		private string _label;
		/// <summary>
		/// Legend item label.
		/// </summary>
		/// <value>The label.</value>
		public string Label
		{
			get
			{
				return _label;
			}
			internal set
			{
				if (_label != value)
				{
					_label = value;
					OnPropertyChanged("Label");
				}
			}
		} 
		#endregion

		#region Symbol
		private Symbol _symbol;

		/// <summary>
		/// Gets the symbol that represents this instance 
		/// </summary>
		public Symbol Symbol
		{
			get { return _symbol; }
			internal set
			{
				if (_symbol != value)
				{
					_symbol = value;
					OnPropertyChanged("Symbol");
				}
			}
		}
		#endregion

		#region Description

		private string _description;

		/// <summary>
		/// The description of the legend item.
		/// </summary>
		public string Description
		{
			get { return _description; }
			internal set
			{
				if (_description != value)
				{
					_description = value;
					OnPropertyChanged("Description");
				}
			}
		}

		#endregion

		#region LayerItemsSource
		/// <summary>
		/// Gets the children of the legend item. 
		/// </summary>
		/// <value>The children. Note that a basic item returns always null but the method can be overridden by inherited classes.</value>
		public virtual IEnumerable<LegendItemViewModel> LayerItemsSource
		{
			get
			{
				return null;
			}
		} 
		#endregion

		#region IsSelected
		private bool _isSelected;
		/// <summary>
		/// Gets or sets a value indicating whether this instance is selected.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is selected; otherwise, <c>false</c>.
		/// </value>
		public bool IsSelected
		{
			get { return _isSelected; }
			internal set
			{
				_isSelected = value;
				OnPropertyChanged("IsSelected");
			}
		}
		#endregion

		#region IsExpanded
		private bool _isExpanded = true;
		/// <summary>
		/// Gets or sets a value indicating whether this instance is expanded.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is expanded; otherwise, <c>false</c>.
		/// </value>
		public bool IsExpanded
		{
			get { return _isExpanded; }
			internal set
			{
				_isExpanded = value;
				OnPropertyChanged("IsExpanded");
			}
		} 
		#endregion

		#region Template
		private DataTemplate _template;
		/// <summary>
		/// Gets or sets the template for this legend item. Depending on the type of item, the template can be the <see cref="Legend.LegendItemTemplate"/> , the <see cref="Legend.LayerTemplate"/>
		/// or the <see cref="Legend.MapLayerTemplate"/>.
		/// The template can also be set explicitely by code.
		/// </summary>
		/// <value>The template.</value>
		public DataTemplate Template
		{
			get
			{
				return _template;
			}
			internal set
			{
				if (value != _template)
				{
					_template = value;
					OnPropertyChanged("Template");
				}
			}
		}

		internal virtual DataTemplate GetTemplate()
		{
			return LegendTree == null ? null : LegendTree.LegendItemTemplate;
		} 
		#endregion

		#region Tag
		private object _tag;
		/// <summary>
		/// Gets or sets an object associated with the legend item.
		/// This provides the ability to attach an arbitrary object to the item.
		/// </summary>
		/// <value>A System.Object that is attached or associated with a legend item..</value>
		public object Tag
		{
			get
			{
				return _tag;
			}
			set
			{
				if (_tag != value)
				{
					_tag = value;
					OnPropertyChanged("Tag");
				}
			}
		}
		#endregion

		#region GeometryType
		private GeometryType _geometryType;

		/// <summary>
		/// The symbol geometry type (<see cref="ArcGISRuntime.Geometry.GeometryType.Unknown"/> by default).
		/// <para>
		/// If the geometry type is <see cref="ArcGISRuntime.Geometry.GeometryType.Unknown"/>, the geometry type will be deduced from the symbol type.
		/// However, in some cases such as a MarkerSymbol used to symbolize a line or an area object, it may be usefull to set this parameter.
		/// </para>
		/// </summary>
		public GeometryType GeometryType
		{
			get { return _geometryType; }
			private set
			{
				if (_geometryType != value)
				{
					_geometryType = value;
					OnPropertyChanged("GeometryType");
				}
			}
		}

		#endregion

		#region internal LegendTree/Attach/Detach
		/// <summary>
		/// Gets or sets the parent legend tree.
		/// </summary>
		/// <value>The legend tree.</value>
		internal LegendTree LegendTree { get; set; }

		internal virtual void Attach(LegendTree legendTree)
		{
			LegendTree = legendTree;
			if (legendTree != null)
			{
				Template = GetTemplate();
			}
		}

		internal virtual void Detach()
		{
			LegendTree = null;
		} 
		#endregion

		#region INotifyPropertyChanged Members
		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		internal void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = PropertyChanged;
			if (propertyChanged != null)
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}

}
