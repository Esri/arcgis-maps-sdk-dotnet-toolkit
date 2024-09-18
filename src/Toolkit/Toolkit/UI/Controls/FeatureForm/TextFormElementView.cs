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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
#if MAUI
using TextBox = Microsoft.Maui.Controls.InputView;
#elif WPF
using System.Windows.Controls.Primitives;
using System.Windows.Input;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Text view for a <see cref="TextFormElement"/> instance.
    /// </summary>
    public partial class TextFormElementView
    {
        private const string TextAreaName = "TextArea";
        private WeakEventListener<TextFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes an instance of the <see cref="TextFormElementView"/> class.
        /// </summary>
        public TextFormElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(TextFormElementView);
#endif
        }

        /// <summary>
        /// Gets or sets the FieldFormElement.
        /// </summary>
        public TextFormElement? Element
        {
            get { return (TextFormElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(TextFormElement), typeof(TextFormElementView), null, propertyChanged: (s, oldValue, newValue) => ((TextFormElementView)s).OnElementPropertyChanged(oldValue as TextFormElement, newValue as TextFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(TextFormElement), typeof(TextFormElementView), new PropertyMetadata(null, (s,e) => ((TextFormElementView)s).OnElementPropertyChanged(e.OldValue as TextFormElement, e.NewValue as TextFormElement)));
#endif

        private void OnElementPropertyChanged(TextFormElement? oldValue, TextFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<TextFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Element_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            UpdateText();
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (_textContainer != null)
            {
#if MAUI
                _textContainer.IsVisible = Element?.IsVisible == true;
#else
                _textContainer.Visibility = Element?.IsVisible == true ? Visibility.Visible : Visibility.Collapsed;
#endif
            }
        }

        private static string RemoveMarkdown(string markdownText)
        {
            return Regex.Replace(markdownText, @"(\*\*|__)(.*?)\1", "$2")
                        .Replace("*", "")
                        .Replace("_", "")
                        .Replace("`", "")
                        .Replace("~", "")
                        .Replace("<br>", "\n")
                        .Replace("<br/>", "\n")
                        .Replace("\n", Environment.NewLine);
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextFormElement.Text))
            {
                this.Dispatch(UpdateText);
            }
            else if (e.PropertyName == nameof(TextFormElement.Format))
            {
                this.Dispatch(UpdateText);
            }
            else if(e.PropertyName == nameof(TextFormElement.IsVisible))
            {
                this.Dispatch(UpdateVisibility);
            }
        }
    }
}