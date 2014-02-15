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
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Manages a set of DependencyPropertyChangedListener by source
    /// </summary>
    /// <typeparam name="TInstance">The type of the instance.</typeparam>
    internal class DependencyPropertyChangedListeners<TInstance> where TInstance : class
    {
        private readonly Dictionary<DependencyObject, IList<DependencyPropertyChangedListener<TInstance>>> _weakEventListenersDict 
            = new Dictionary<DependencyObject, IList<DependencyPropertyChangedListener<TInstance>>>();

        /// <summary>
        /// WeakReference to the instance listening for the DP changed.
        /// </summary>
        private readonly WeakReference _weakInstance;

        public DependencyPropertyChangedListeners(TInstance instance)
        {
            if (null == instance)
            {
                throw new ArgumentNullException("instance");
            }
            _weakInstance = new WeakReference(instance);
        }

        /// <summary>
        /// Gets or sets the method to call when the DP changes.
        /// </summary>
        public Action<TInstance, DependencyObject, PropertyChangedEventArgs> OnEventAction { get; set; }

        /// <summary>
        /// Starts listening for the DP changed of a DO
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyName">Name of the property.</param>
        public void Attach(DependencyObject source, string propertyName)
        {
            var target = (TInstance)_weakInstance.Target;

            if (null != target)
            {
                IList<DependencyPropertyChangedListener<TInstance>> weakEventListeners;
                if (_weakEventListenersDict.ContainsKey(source))
                {
                    weakEventListeners = _weakEventListenersDict[source];
                }
                else
                {
                    weakEventListeners = new List<DependencyPropertyChangedListener<TInstance>>();
                    _weakEventListenersDict[source] = weakEventListeners;
                }

                weakEventListeners.Add(new DependencyPropertyChangedListener<TInstance>(target, source, propertyName){ OnEventAction = OnEventAction});
            }
            else
                Debug.Assert(false, "A disposed instance tries to listen");

        }

        public void Detach(DependencyObject source)
        {
            if (_weakEventListenersDict.ContainsKey(source))
            {
                var weakEventListeners = _weakEventListenersDict[source];
                foreach(var wel in weakEventListeners)
                    wel.Detach();
                _weakEventListenersDict.Remove(source);
            }
            else
            {
                Debug.Assert(false, "Trying to detach inexisting WeakEventListener");
            }
        }

        public void DetachAll()
        {
            foreach (DependencyPropertyChangedListener<TInstance> wel in _weakEventListenersDict.Values.SelectMany(l => l).ToArray())
            {
                wel.Detach();
            }
            _weakEventListenersDict.Clear();
        }
    }
}
