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

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Supporting selector for the <see cref="FeatureFormView"/> control,
/// used for rendering a set of form elements.
/// </summary>
public class FormElementTemplateSelector : DataTemplateSelector
{
    static FormElementTemplateSelector()
    {
        DefaultFieldFormElementTemplate = new DataTemplate(() =>
        {
            var view = new FieldFormElementView();
            view.SetBinding(FieldFormElementView.ElementProperty, Binding.SelfPath);
            return view;
        });
        DefaultGroupFormElementTemplate = new DataTemplate(() =>
        {
            VerticalStackLayout itemsView = new();
            itemsView.Margin = new Thickness(0, 10);
            itemsView.SetBinding( // Use the selector from FeatureFormView
                BindableLayout.ItemTemplateSelectorProperty,
                new Binding(
                    nameof(FeatureFormView.FormElementTemplateSelector),
                    source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestor, typeof(FeatureFormView))));
            BindableLayout.SetItemTemplateSelector(itemsView, new FormElementTemplateSelector());
            itemsView.SetBinding(BindableLayout.ItemsSourceProperty, nameof(GroupFormElement.Elements));
            return itemsView;
        });
    }

    /// <inheritdoc/>
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            FieldFormElement => FieldFormElementTemplate,
            GroupFormElement => GroupFormElementTemplate,
            _ => new DataTemplate(),
        };
    }

    /// <summary>
    /// Gets or sets the template used for rendering a <see cref="FieldFormElement"/>.
    /// </summary>
    /// <seealso cref="FieldFormElement"/>
    /// <seealso cref="FieldFormElementView"/>
    public DataTemplate FieldFormElementTemplate { get; set; } = DefaultFieldFormElementTemplate;

    /// <summary>
    /// Template used for rendering a <see cref="GroupFormElement"/>.
    /// </summary>
    /// <seealso cref="GroupFormElement"/>
    public DataTemplate GroupFormElementTemplate { get; set; } = DefaultGroupFormElementTemplate;

    private static readonly DataTemplate DefaultFieldFormElementTemplate;
    private static readonly DataTemplate DefaultGroupFormElementTemplate;
}
#endif
