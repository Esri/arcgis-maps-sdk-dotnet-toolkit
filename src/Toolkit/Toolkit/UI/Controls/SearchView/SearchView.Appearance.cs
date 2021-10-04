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
#if !XAMARIN
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if !NETFX_CORE
using System.Windows.Controls;
using System.Windows;
#else
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// View for searching with locators or custom search sources.
    /// </summary>
    public partial class SearchView : Control, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the style applied to the search button.
        /// </summary>
        public Style SearchButtonStyle
        {
            get => (Style)GetValue(SearchButtonStyleProperty);
            set => SetValue(SearchButtonStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to the source selection button.
        /// </summary>
        public Style SourceSelectButtonStyle
        {
            get => (Style)GetValue(SourceSelectButtonStyleProperty);
            set => SetValue(SourceSelectButtonStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to the clear button.
        /// </summary>
        public Style ClearButtonStyle
        {
            get => (Style)GetValue(ClearButtonStyleProperty);
            set => SetValue(ClearButtonStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to the placeholder text block.
        /// </summary>
        public Style PlaceholderTextBlockStyle
        {
            get => (Style)GetValue(PlaceholderTextBlockStyleProperty);
            set => SetValue(PlaceholderTextBlockStyleProperty, value);
        }

        /// <summary>
        ///  Gets or sets the style applied to the query entry textbox.
        /// </summary>
        public Style QueryTextBoxStyle
        {
            get => (Style)GetValue(QueryTextBoxStyleProperty);
            set => SetValue(QueryTextBoxStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the templated used to display suggestions.
        /// </summary>
        public DataTemplate SearchSuggestionTemplate
        {
            get => (DataTemplate)GetValue(SearchSuggestionTemplateProperty);
            set => SetValue(SearchSuggestionTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used to display search results.
        /// </summary>
        public DataTemplate SearchResultTemplate
        {
            get => (DataTemplate)GetValue(SearchResultTemplateProperty);
            set => SetValue(SearchResultTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to the suggestion Popup.
        /// </summary>
        public Style SuggestionPopupStyle
        {
            get => (Style)GetValue(SuggestionPopupStyleProperty);
            set => SetValue(SuggestionPopupStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to the outer border.
        /// </summary>
        public Style SearchBarBorderStyle
        {
            get => (Style)GetValue(SearchBarBorderStyleProperty);
            set => SetValue(SearchBarBorderStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SourceSelectButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceSelectButtonStyleProperty =
            DependencyProperty.Register(nameof(SourceSelectButtonStyle), typeof(Style), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ClearButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearButtonStyleProperty =
            DependencyProperty.Register(nameof(ClearButtonStyle), typeof(Style), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="PlaceholderTextBlockStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextBlockStyleProperty =
            DependencyProperty.Register(nameof(PlaceholderTextBlockStyle), typeof(Style), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="QueryTextBoxStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty QueryTextBoxStyleProperty =
            DependencyProperty.Register(nameof(QueryTextBoxStyle), typeof(Style), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="SearchSuggestionTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchSuggestionTemplateProperty =
            DependencyProperty.Register(nameof(SearchSuggestionTemplate), typeof(DataTemplate), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="SearchResultTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchResultTemplateProperty =
            DependencyProperty.Register(nameof(SearchResultTemplate), typeof(DataTemplate), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="SuggestionPopupStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SuggestionPopupStyleProperty =
            DependencyProperty.Register(nameof(SuggestionPopupStyle), typeof(Style), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="SearchBarBorderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchBarBorderStyleProperty =
            DependencyProperty.Register(nameof(SearchBarBorderStyle), typeof(Style), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="SearchButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchButtonStyleProperty =
            DependencyProperty.Register(nameof(SearchButtonStyle), typeof(Style), typeof(SearchView), new PropertyMetadata(null));
    }
}
#endif