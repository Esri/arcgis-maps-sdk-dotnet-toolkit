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
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    internal class FeatureFormElementTemplateSelector : DataTemplateSelector
    {
        private static DataTemplate DefaultFieldFormElementTemplate;
        private static DataTemplate DefaultGroupElementTemplate;

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.IsVisible), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Label), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Description), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement.Elements), "Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement", "Esri.ArcGISRuntime")]
        static FeatureFormElementTemplateSelector()
        {
            DefaultFieldFormElementTemplate = new DataTemplate(() =>
            {
                var view = new FieldFormElementView() { Margin = new Thickness(0, 0) };
                view.SetBinding(FieldFormElementView.ElementProperty, Binding.SelfPath);
                return view;
            });
            DefaultGroupElementTemplate = new DataTemplate(() =>
            {
                // TODO: Make expandable/collapsible
                var layout = new VerticalStackLayout();
                layout.SetBinding(VerticalStackLayout.IsVisibleProperty, nameof(FormElement.IsVisible));
                var label = new Label() { Margin = new Thickness(0, 10), Style = FeatureFormView.GetFeatureFormTitleStyle() };
                label.SetBinding(Label.TextProperty, nameof(FormElement.Label));
                layout.Children.Add(label);
                label = new Label() { Margin = new Thickness(0, 10), Style = FeatureFormView.GetFeatureFormCaptionStyle() };
                label.SetBinding(Label.TextProperty, nameof(FormElement.Description));
                layout.Children.Add(label);
                VerticalStackLayout itemsView = new VerticalStackLayout()
                {
                    Margin = new Thickness(0, 10),
                };
                BindableLayout.SetItemTemplateSelector(itemsView, new FeatureFormElementTemplateSelector());
                itemsView.SetBinding(BindableLayout.ItemsSourceProperty, nameof(GroupFormElement.Elements));
                layout.Children.Add(itemsView);
                return layout;
            });
        }

        public FeatureFormElementTemplateSelector()
        {
            FieldFormElementTemplate = DefaultFieldFormElementTemplate;
            GroupFormElementTemplate = DefaultGroupElementTemplate;
        }

        public DataTemplate FieldFormElementTemplate { get; set; }

        public DataTemplate GroupFormElementTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if(item is FieldFormElement)
            {
                return FieldFormElementTemplate;
            }
            else if(item is GroupFormElement)
            {
                return GroupFormElementTemplate;
            }
            return null!;
        }
    }
}
#endif