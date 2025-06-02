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
using Esri.ArcGISRuntime.Mapping.FeatureForms;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationsFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationsFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var root = new VerticalStackLayout();
            root.SetBinding(VerticalStackLayout.IsVisibleProperty, static (UtilityAssociationsFormElementView view) => view.Element?.IsVisible);
            var label = new Label();
            label.SetBinding(Label.TextProperty, static (UtilityAssociationsFormElementView view) => view.Element?.Label, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(View.IsVisibleProperty, static (Label lbl) => lbl.Text, source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter());
            label.Style = FeatureFormView.GetFeatureFormTitleStyle();
            root.Children.Add(label);
            root.Children.Add(new Label() { Text = "UtilityAssociationsFormElement content goes here" }); // TODO
            label = new Label();
            label.SetBinding(Label.TextProperty, static (UtilityAssociationsFormElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(Label.IsVisibleProperty, static (Label lbl) => lbl.Text, source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter());
            label.Style = FeatureFormView.GetFeatureFormCaptionStyle();
            root.Children.Add(label);
            
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            return root;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
#endif