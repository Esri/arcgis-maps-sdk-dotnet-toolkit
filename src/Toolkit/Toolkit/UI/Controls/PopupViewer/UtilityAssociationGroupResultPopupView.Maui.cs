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
    public partial class UtilityAssociationGroupResultPopupView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationGroupResultPopupView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationGroupResultPopupView view) => view.GroupResult?.AssociationResults, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);

            INameScope nameScope = new NameScope();
            nameScope.RegisterName("ResultsList", cv);
            return cv;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid() { Padding = new Thickness(10, 0, 0, 0) };
            TapGestureRecognizer itemTapGesture = new TapGestureRecognizer();
            itemTapGesture.Tapped += Result_Tapped;
            layout.GestureRecognizers.Add(itemTapGesture);

            var view = new UtilityAssociationResultPopupView();
            view.SetBinding(UtilityAssociationResultPopupView.AssociationResultProperty, static (UtilityNetworks.UtilityAssociationResult result) => result);
            layout.Add(view);
            return layout;
        }

        private static void Result_Tapped(object? sender, EventArgs e)
        {
            var cell = sender as View;
            if (cell?.BindingContext is UtilityNetworks.UtilityAssociationResult result)
            {
                var parent = PopupViewer.GetPopupViewerParent(cell);
                parent?.NavigateToItem(Popup.FromGeoElement(result.AssociatedFeature)); 
            }
        }
    }
}
#endif