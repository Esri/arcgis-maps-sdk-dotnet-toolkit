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

using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
#if !MAUI
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Class used to represent an entry in the Legend control.
    /// </summary>
    /// <remarks>
    /// The <see cref="Content"/> property will contain the actual object it represents, mainly <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.
    /// </remarks>
    public class LegendEntry : object, ILayerContentItem, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LegendEntry"/> class.
        /// </summary>
        /// <param name="content">The object this entry represents, usually a <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.</param>
        public LegendEntry(object content)
        {
            Content = content;
            if (content is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += Content_PropertyChanged;
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">name of the property changing</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Content_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILayerContent.Name) || e.PropertyName == nameof(LegendInfo.Symbol))
                PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Gets the content that this entry represents, usually a <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.
        /// </summary>
        public object Content { get; }

        /// <summary>
        /// Gets the display name of the content
        /// </summary>
        public string Name
        {
            get
            {
                if (Content is Layer l)
                    return l.Name;
                if (Content is ILayerContent lc)
                    return lc.Name;
                if (Content is LegendInfo li)
                    return li.Name;
#if !MAUI
                if (Content is DesignLegendInfo dli)
                    return dli.Name;
#endif
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the symbol of the content if it's a LegendInfo
        /// </summary>
        public Symbology.Symbol? Symbol
        {
            get
            {
                if (Content is LegendInfo li)
                    return li.Symbol;
#if !MAUI
                if (Content is DesignLegendInfo dli)
                    return dli.Symbol;
#endif
                return null;
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (Content != null)
            {
                return Content.GetHashCode();
            }

            return base.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is LegendEntry le && ReferenceEquals(Content, le.Content);
    }
}