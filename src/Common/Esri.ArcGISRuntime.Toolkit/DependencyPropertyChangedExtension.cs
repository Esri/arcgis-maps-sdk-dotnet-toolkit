// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit
{
	// Observe property changed event
	// Note that the use of Attached properties is not working on Desktop where an attached property can be created only once  --> use of internal relay objects that captures the changes
	internal static class DependencyPropertyChangedExtensions
	{
		internal static DependencyProperty HandlerListProperty;
		internal static object LockList = new object();

		static DependencyPropertyChangedExtensions()
		{
			try
			{
				HandlerListProperty = DependencyProperty.RegisterAttached("ToolkitHandlerList", typeof(List<Tuple<RelayObject, string, Action<object, PropertyChangedEventArgs>>>), typeof(DependencyObject), null);
			}
			catch // can happen in design mode when the dll is reloaded
			{
			}
		}
		internal static void AddPropertyChangedHandler(this DependencyObject dependencyObject, string propertyName, Action<object, PropertyChangedEventArgs> onPropertyChanged)
		{
			if (HandlerListProperty == null)
				return;
			CompatUtility.ExecuteOnUIThread(() => AddPropertyChangedHandlerImpl(dependencyObject, propertyName, onPropertyChanged));
		}

		internal static void RemovePropertyChangedHandler(this DependencyObject dependencyObject, string propertyName, Action<object, PropertyChangedEventArgs> onPropertyChanged)
		{
			if (HandlerListProperty == null)
				return;
			CompatUtility.ExecuteOnUIThread(() => RemovePropertyChangedHandlerImpl(dependencyObject, propertyName, onPropertyChanged));
		}

		private static void AddPropertyChangedHandlerImpl(DependencyObject dependencyObject, string propertyName, Action<object, PropertyChangedEventArgs> onPropertyChanged)
		{
			var relayObject = new RelayObject(dependencyObject, propertyName, onPropertyChanged);

			// store the relayObject created for observing the property changes (to be able to unsubscribe)
			lock (LockList)
			{
				var handlerList = dependencyObject.GetValue(HandlerListProperty) as List<Tuple<RelayObject, string, Action<object, PropertyChangedEventArgs>>>
					?? new List<Tuple<RelayObject, string, Action<object, PropertyChangedEventArgs>>>();
				handlerList.Add(new Tuple<RelayObject, string, Action<object, PropertyChangedEventArgs>>(relayObject, propertyName, onPropertyChanged));
				dependencyObject.SetValue(HandlerListProperty, handlerList);
			}
		}

		private static void RemovePropertyChangedHandlerImpl(DependencyObject dependencyObject, string propertyName, Action<object, PropertyChangedEventArgs> onPropertyChanged)
		{
			// retrieve the relay object that had been created to observe the changes
			lock (LockList)
			{
				var handlerList = dependencyObject.GetValue(HandlerListProperty) as List<Tuple<RelayObject, string, Action<object, PropertyChangedEventArgs>>>;
				Debug.Assert(handlerList != null); // no reason to call RemovePropertyChangedHandler if AddPropertyChangedHandler has never been called
				if (handlerList != null)
				{
					var tuple = handlerList.FirstOrDefault(t => t.Item2 == propertyName && t.Item3 == onPropertyChanged);
					Debug.Assert(tuple != null); // no reason to call RemovePropertyChangedHandler if AddPropertyChangedHandler has never been called
					if (tuple != null)
					{
						RelayObject relayObject = tuple.Item1;
						relayObject.Dispose();
						handlerList.Remove(tuple);
						dependencyObject.SetValue(HandlerListProperty, handlerList.Any() ? handlerList : null);
					}
				}
			}
		}

		// RelayObject with a property (Value) binded to the property to observe.
		private sealed class RelayObject : DependencyObject, IDisposable
		{
			private readonly Action<object, PropertyChangedEventArgs> _onPropertyChanged;
			private DependencyObject _dependencyObject;
			private readonly string _propertyName;

			public RelayObject(DependencyObject dependencyObject, string propertyName, Action<object, PropertyChangedEventArgs> onPropertyChanged)
			{
				_propertyName = propertyName;

				// Bind Value property to the property to observe
				var binding = new Binding
				{
					Path = new PropertyPath(propertyName),
					Source = dependencyObject,
					Mode = BindingMode.OneWay
				};
				BindingOperations.SetBinding(this, ValueProperty, binding);
				_onPropertyChanged = onPropertyChanged; // Note set _onPropertyChanged after SetBinding so event not fired for the initial value
				_dependencyObject = dependencyObject;
			}

			private static readonly DependencyProperty ValueProperty =
				DependencyProperty.Register("Value", typeof(object), typeof(RelayObject), new PropertyMetadata(null, OnPropertyChanged));

			static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			{
				((RelayObject)d).OnPropertyChanged();
			}
			private void OnPropertyChanged()
			{
				if (_onPropertyChanged != null)
				{
					Debug.Assert(_dependencyObject != null);
					_onPropertyChanged(_dependencyObject, new PropertyChangedEventArgs(_propertyName));
				}
			}

			public void Dispose()
			{
				if (_dependencyObject != null)
				{
					BindingOperations.SetBinding(this, ValueProperty, new Binding());  // No WinRT ClearBinding so set an empty binding instead
					_dependencyObject = null;
				}
			}
		}
	}

}
