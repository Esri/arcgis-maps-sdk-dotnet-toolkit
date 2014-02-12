// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Manages a collection of WeakEventListener for listening for events coming from multiple sources.
    /// </summary>
    /// <typeparam name="TInstance">The type of the instance.</typeparam>
    internal class WeakEventListeners<TInstance> where TInstance : class
    {
        private readonly Dictionary<object, WeakEventListener<TInstance, object, EventArgs>> _weakEventListenersDict
            = new Dictionary<object, WeakEventListener<TInstance, object, EventArgs>>();

        /// <summary>
        /// WeakReference to the instance listening for the event.
        /// </summary>
        private readonly WeakReference _weakInstance;

        public WeakEventListeners(TInstance instance)
        {
            if (null == instance)
            {
                throw new ArgumentNullException("instance");
            }
            _weakInstance = new WeakReference(instance);
        }

        /// <summary>
        /// Gets or sets the method to call when the event fires.
        /// </summary>
        public Action<TInstance, object, EventArgs> OnEventAction { get; set; }

        /// <summary>
        /// Attaches the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="subscription">The subscription.</param>
        /// <param name="unsubscription">The unsubscription.</param>
        public void Attach(object source, Action<EventHandler> subscription, Action<EventHandler> unsubscription)
        {
            if (_weakEventListenersDict.ContainsKey(source)) // not design for (note we could tweak the code to manage a list of WeakEventListener by source if needed)
            {
                _weakEventListenersDict[source].Detach(); 
                Debug.Assert(false, "Listening twice a source");
            }

            var target = (TInstance)_weakInstance.Target;

            if (null != target)
            {
                var weakEventListener = new WeakEventListener<TInstance, object, EventArgs>(target)
                {
                    OnEventAction = OnEventAction,
                    OnDetachAction = wel => unsubscription(wel.OnEvent)
                };
                subscription(weakEventListener.OnEvent);

                _weakEventListenersDict[source] = weakEventListener;
            }
            else
                Debug.Assert(false, "A disposed instance tries to listen");

        }

        public void Detach(object source)
        {
            WeakEventListener<TInstance, object, EventArgs> weakEventListener = _weakEventListenersDict[source];
            if (weakEventListener != null) 
            {
                weakEventListener.Detach();
                _weakEventListenersDict.Remove(source);
            }
            else
            {
                Debug.Assert(false, "Trying to detach inexisting WeakEventListener");
            }

        }

        public void DetachAll()
        {
            foreach (WeakEventListener<TInstance, object, EventArgs> wel in _weakEventListenersDict.Values.ToArray())
            {
                wel.Detach();
            }
            _weakEventListenersDict.Clear();
        }
    }
}