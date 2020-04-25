// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal
#else
namespace Esri.ArcGISRuntime.Toolkit.Internal
#endif
{
    /// <summary>
    /// Implements a weak event listener that allows the owner to be garbage
    /// collected if its only remaining link is an event handler.
    /// </summary>
    /// <typeparam name="TInstance">Type of instance listening for the event.</typeparam>
    /// <typeparam name="TSource">Type of source for the event.</typeparam>
    /// <typeparam name="TEventArgs">Type of event arguments for the event.</typeparam>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Used as link target in several projects.")]
#pragma warning disable SA1402 // File may only contain a single class
    internal class WeakEventListener<TInstance, TSource, TEventArgs>
#pragma warning restore SA1402 // File may only contain a single class
        where TInstance : class
    {
        /// <summary>
        /// WeakReference to the instance listening for the event.
        /// </summary>
        private WeakReference _weakInstance;

        /// <summary>
        /// Gets or sets the method to call when the event fires.
        /// </summary>
        public Action<TInstance, TSource, TEventArgs> OnEventAction { get; set; }

        /// <summary>
        /// Gets or sets the method to call when detaching from the event.
        /// </summary>
        public Action<TInstance, WeakEventListener<TInstance, TSource, TEventArgs>> OnDetachAction { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakEventListener{TInstance, TSource, TEventArgs}"/> class.
        /// </summary>
        /// <param name="instance">Instance subscribing to the event.</param>
        public WeakEventListener(TInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            _weakInstance = new WeakReference(instance);
        }

        /// <summary>
        /// Handler for the subscribed event calls OnEventAction to handle it.
        /// </summary>
        /// <param name="source">Event source.</param>
        /// <param name="eventArgs">Event arguments.</param>
        public void OnEvent(TSource source, TEventArgs eventArgs)
        {
            TInstance target = (TInstance)_weakInstance.Target;
            if (target != null)
            {
                // Call registered action
                OnEventAction?.Invoke(target, source, eventArgs);
            }
            else
            {
                // Detach from event
                Detach();
            }
        }

        /// <summary>
        /// Detaches from the subscribed event.
        /// </summary>
        public void Detach()
        {
            if (_weakInstance?.Target is TInstance target)
            {
                OnDetachAction?.Invoke(target, this);
            }

            OnDetachAction = null;
        }
    }

    /// <summary>
    /// Implements a weak event listener that allows the owner to be garbage
    /// collected if its only remaining link is an event handler.
    ///
    /// USAGE:
    ///
    /// EventReceiver - the class that is listening for the event
    /// eventSource - the object that is firing a 'Changed' event.
    /// _eventListener - an instance of WeakEventListener.
    ///
    /// SUBSCRIBE TO EVENT:
    ///
    /// _eventListener = new WeakEventListener&lt;EventReceiver&gt;(this)
    ///     {
    ///         OnEventAction = l => /* do something to handle the event */,
    ///         OnDetachAction = wel => newValue.Changed -= wel.OnEvent
    ///     };
    /// eventSource.Changed += _eventListener.OnEvent;
    ///
    /// UNSUBSCRIBE FROM EVENT:
    ///
    /// _eventListener.Detach();
    /// _eventListener = null;
    ///
    /// </summary>
    /// <typeparam name="TInstance">Type of instance listening for the event.</typeparam>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Used as link target in several projects.")]
#pragma warning disable SA1402
    internal class WeakEventListener<TInstance>
        where TInstance : class
#pragma warning restore SA1402
    {
        /// <summary>
        /// WeakReference to the instance listening for the event.
        /// </summary>
        private WeakReference<TInstance> _weakInstance;

        /// <summary>
        /// Gets or sets the method to call when the event fires.
        /// </summary>
        public Action<TInstance> OnEventAction { get; set; }

        /// <summary>
        /// Gets or sets the method to call when detaching from the event.
        /// </summary>
        public Action<WeakEventListener<TInstance>> OnDetachAction { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakEventListener{TInstance}"/> class.
        /// </summary>
        /// <param name="instance">Instance subscribing to the event.</param>
        public WeakEventListener(TInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            _weakInstance = new WeakReference<TInstance>(instance);
        }

        /// <summary>
        /// Handler for the subscribed event calls OnEventAction to handle it.
        /// </summary>
        public void OnEvent()
        {
            TInstance target;
            if (_weakInstance.TryGetTarget(out target))
            {
                // Call registered action
                if (OnEventAction != null)
                {
                    OnEventAction(target);
                }
            }
            else
            {
                // Detach from event
                Detach();
            }
        }

        /// <summary>
        /// Detaches from the subscribed event.
        /// </summary>
        public void Detach()
        {
            if (OnDetachAction != null)
            {
                OnDetachAction(this);
                OnDetachAction = null;
            }
        }
    }
}