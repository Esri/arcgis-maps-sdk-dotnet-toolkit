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

        static FieldsPopupElementView()
        {
            string template = """
<ControlTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui" xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" 
    xmlns:esriP="clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui.Primitives"
    x:DataType="esriP:FieldsPopupElementView" x:Name="Self">
     <ContentPresenter x:Name="TableAreaContent" />
</ControlTemplate>
""";
            DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);

            DefaultFieldTextStyle = new Style(typeof(Label));
            DefaultFieldTextStyle.Setters.Add(new Setter() { Property = Label.MarginProperty, Value = new Thickness(7) });
            DefaultFieldTextStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 12d });
            DefaultFieldTextStyle.Setters.Add(new Setter() { Property = Label.TextColorProperty, Value = Color.FromRgb(0x32, 0x32, 0x32) });
        }
    }
}
#endif