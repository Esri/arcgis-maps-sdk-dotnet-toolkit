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

#if WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;


#if WINUI
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.Text;
#elif WINDOWS_UWP   
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents;
#endif


namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="TextPopupElement"/>.
    /// </summary>
    [TemplatePart(Name = TextAreaName, Type = typeof(ContentControl))]
    public partial class TextPopupElementView : Control
    {
        private const string TextAreaName = "TextArea";

        private void OnElementPropertyChanged()
        {
            // Full list of supported tags and attributes here: https://doc.arcgis.com/en/arcgis-online/reference/supported-html.htm
            if (!string.IsNullOrEmpty(Element?.Text) && GetTemplateChild(TextAreaName) is ContentControl rtb)
            {
                rtb.Content = HtmlToView.ToUIElement(Element.Text, (s, e) => PopupViewer.GetPopupViewerParent(s as DependencyObject)?.OnHyperlinkClicked(e));
                rtb.Visibility = string.IsNullOrEmpty(Element?.Text) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}
#endif