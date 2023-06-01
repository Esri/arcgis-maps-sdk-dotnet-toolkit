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

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class PopupViewer : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static PopupViewer()
        {
            string template = """
<ControlTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui" xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" 
    xmlns:esriTK="clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui;assembly=Esri.ArcGISRuntime.Toolkit.Maui" xmlns:esriP="clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui.Primitives;assembly=Esri.ArcGISRuntime.Toolkit.Maui"
    x:DataType="controls:PopupViewer" x:Name="Self">
    <Grid BindingContext="{TemplateBinding Popup}">
           <Grid.RowDefinitions>
             <RowDefinition Height="Auto"/>
             <RowDefinition Height="*"/>
           </Grid.RowDefinitions>
       <Label Text="{Binding Title}" />
       <ScrollView VerticalScrollBarVisibility="{TemplateBinding VerticalScrollBarVisibility}" Grid.Row="1" x:Name="PopupContentScrollViewer">
           <CollectionView ItemsSource="{Binding EvaluatedElements}" Margin="0,10" x:Name="ItemsView">
               <CollectionView.ItemTemplate>
                   <esriP:PopupElementTemplateSelector>
                       <esriP:PopupElementTemplateSelector.TextPopupElementTemplate>
                           <DataTemplate>
                               <Label Text="TextPopupElementTemplate" />
                           </DataTemplate>
                       </esriP:PopupElementTemplateSelector.TextPopupElementTemplate>
                   </esriP:PopupElementTemplateSelector>
               </CollectionView.ItemTemplate>
           </CollectionView>
       </ScrollView>
   </Grid>
</ControlTemplate>
""";
            DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);
        }
    }
}
#endif