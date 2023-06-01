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
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Markdig.Renderers.Html.Inlines;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="TextPopupElement"/>.
    /// </summary>
    public partial class TextPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static TextPopupElementView()
        {
            string template = """
<ControlTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui" xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" 
    xmlns:esriP="clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui.Primitives"
    x:DataType="esriP:TextPopupElementView" x:Name="Self">
     <Label x:Name="TextArea" />
</ControlTemplate>
""";
            DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);
        }

        private void OnElementPropertyChanged()
        {
            var label = GetTemplateChild("TextArea") as Label;
            if (label is null) return;
#if !WINDOWS
            label.TextType = TextType.Html;
#endif
            label.Text = Element?.Text;
        }
    }
}
#endif