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

#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="FieldsPopupElement"/>.
    /// </summary>
    public partial class FieldsPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly Style DefaultFieldTextStyle;

        /// <summary>
        /// Template name of the <see cref="Label"/> text area.
        /// </summary>
        public const string TableAreaContentName = "TableAreaContent";

        static FieldsPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
            DefaultFieldTextStyle = new Style(typeof(Label));
            DefaultFieldTextStyle.Setters.Add(new Setter() { Property = Label.MarginProperty, Value = new Thickness(7) });
            DefaultFieldTextStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 12d });
            DefaultFieldTextStyle.Setters.Add(new Setter() { Property = Label.TextColorProperty, Value = Color.FromRgb(0x32, 0x32, 0x32) });
        }

        private static object BuildDefaultTemplate()
        {
            var presenter = new ContentPresenter();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(presenter, nameScope);
            nameScope.RegisterName(TableAreaContentName, presenter);
            return presenter;
        }
    }

#if WINDOWS
    // Enables the text cells to be selectable for MAUI-on-WinUI.
    internal sealed partial class SelectableLabel : Microsoft.Maui.Controls.Label
    {
        protected override void OnHandlerChanged()
        {
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
            {
                textBlock.IsTextSelectionEnabled = true;
            }
            base.OnHandlerChanged();
        }
    }
#endif
}
#endif