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

using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Implements a weak event listener that allows the owner to be garbage
    /// collected if its only remaining link is an event handler.
    /// </summary>
    /// <typeparam name="TListeningInstance">Type of instance listening for the event.</typeparam>
    /// <typeparam name="TEventRaisingSource">Type of source for the event.</typeparam>
    /// <typeparam name="TEventSender">Type of sender for the event handler.</typeparam>
    /// <typeparam name="TEventArgs">Type of event arguments for the event.</typeparam>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Used as link target in several projects.")]
    internal class WeakEventListener<TListeningInstance, TEventRaisingSource, TEventSender, TEventArgs>
        where TListeningInstance : class
    {
        /// <summary>
        /// WeakReference to the instance listening for the event.
        /// </summary>
        private WeakReference _listeningInstance;
        private TEventRaisingSource _eventSource;

        /// <summary>
        /// Gets or sets the method to call when the event fires.
        /// </summary>
        public Action<TListeningInstance, TEventSender?, TEventArgs>? OnEventAction { get; set; }

        /// <summary>
        /// Gets or sets the method to call when detaching from the event.
        /// </summary>
        public Action<TListeningInstance?, TEventRaisingSource, WeakEventListener<TListeningInstance, TEventRaisingSource, TEventSender, TEventArgs>>? OnDetachAction { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakEventListener{TListeningInstance, TEventRaisingSource, TEventSender, TEventArgs}"/> class.
        /// </summary>
        /// <param name="instance">Instance subscribing to the event.</param>
        /// <param name="source">Source of the event.</param>
        public WeakEventListener(TListeningInstance instance, TEventRaisingSource source)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _listeningInstance = new WeakReference(instance);
            _eventSource = source;
        }

        /// <summary>
        /// Handler for the subscribed event calls OnEventAction to handle it.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="eventArgs">Event arguments.</param>
        public void OnEvent(TEventSender? sender, TEventArgs eventArgs)
        {
            TListeningInstance? target = (TListeningInstance?)_listeningInstance.Target;
            if (target != null)
            {
                // Call registered action
                OnEventAction?.Invoke(target, sender, eventArgs);
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
            OnDetachAction?.Invoke(_listeningInstance.Target as TListeningInstance, _eventSource, this);
            OnDetachAction = null;
        }
    }
}