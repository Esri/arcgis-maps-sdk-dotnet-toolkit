// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal class DependencyPropertyChangedListener<TInstance> where TInstance : class
    {
        /// <summary>
        /// WeakReference to the instance listening for the DP changed.
        /// </summary>
        private readonly WeakReference _weakInstance;

        // Realy Object binded to the DP to observe
        private RelayObject _relayObject;

        /// <summary>
        /// Gets or sets the method to call when the DP changes.
        /// </summary>
        public Action<TInstance, DependencyObject, PropertyChangedEventArgs> OnEventAction { get; set; }

        /// <summary>
        /// Initializes a new instances of the DependencyPropertyChangedListener class.
        /// </summary>
        /// <param name="instance">Instance subscribing to the property changed event.</param>
        /// <param name="dependencyObject">Source of the propertyChanged</param>
        /// <param name="propertyName">Dependency property name</param>
        public DependencyPropertyChangedListener(TInstance instance, DependencyObject dependencyObject, string propertyName)
        {
            _relayObject = new RelayObject(dependencyObject, propertyName, OnPropertyChanged);
            _weakInstance = new WeakReference(instance);
        }

        private void OnPropertyChanged(DependencyObject sender, PropertyChangedEventArgs args)
        {
            var target = (TInstance)_weakInstance.Target;
            if (target != null)
            {
                // Call registered action
                if (null != OnEventAction)
                {
                    OnEventAction(target, sender, args);
                }
            }
            else
            {
                // Detach from event
                Detach();
            }
        }

        /// <summary>
        /// Stop listening for the dependency property changed.
        /// </summary>
        public void Detach()
        {
            if (_relayObject != null)
            {
                CompatUtility.ExecuteOnUIThread(() =>
                {
                    if (_relayObject != null)
                    {
                        _relayObject.Dispose();
                        _relayObject = null;
                    }
                });
            }
        }

    }


    // RelayObject with a property (Value) binded to the property to observe.
    internal sealed class RelayObject : DependencyObject, IDisposable
    {
        private Action<DependencyObject, PropertyChangedEventArgs> _onPropertyChanged;
        private DependencyObject _dependencyObject;
        private readonly string _propertyName;

        public RelayObject(DependencyObject dependencyObject, string propertyName, Action<DependencyObject, PropertyChangedEventArgs> onPropertyChanged)
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
            _onPropertyChanged = null;
            if (_dependencyObject != null)
            {
                _dependencyObject = null;
                BindingOperations.SetBinding(this, ValueProperty, new Binding());  // No WinRT ClearBinding so set an empty binding instead
            }
        }
    }
}
