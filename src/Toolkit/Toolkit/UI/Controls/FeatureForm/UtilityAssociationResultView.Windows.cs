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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
#if WINUI
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
#elif WINDOWS_UWP
using Windows.UI;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media;
#elif WPF
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class UtilityAssociationResultView : Control
    {
        private partial void ShowDetailsFlyout(FeatureFormView parent, object? flyoutTarget)
        {
#if WPF
            ContextMenu contextMenu = new ContextMenu();
            if (parent.GeoView is not null)
            {
                contextMenu.Items.Add(CreateMenuItem(
                    Properties.Resources.GetString("FeatureFormUtilityAssociationsShowOnMap"),
                    ToolkitIcons.CenterMap,
                    (s, e) => ShowAssociationOnMap(parent)));
            }

            contextMenu.Items.Add(CreateMenuItem(
                Properties.Resources.GetString("FeatureFormUtilityAssociationsMoreInformation"),
                ToolkitIcons.MoreInformation,
                (s, e) => NavigateToAssociationDetails(parent)));

            if (CanRemoveAssociation(parent))
            {
                contextMenu.Items.Add(CreateMenuItem(
                    Properties.Resources.GetString("FeatureFormUtilityAssociationsRemoveAssociation"),
                    ToolkitIcons.Delete,
                    (s, e) => _ = RemoveAssociation(AssociationResult?.Association, parent),
                    Brushes.Red));
            }

            contextMenu.PlacementTarget = flyoutTarget as UIElement ?? this;
            contextMenu.IsOpen = true;
#elif WINDOWS_XAML
            MenuFlyout contextMenu = new();
            if (parent.GeoView is not null)
            {
                contextMenu.Items.Add(CreateMenuFlyoutItem(
                    Properties.Resources.GetString("FeatureFormUtilityAssociationsShowOnMap"),
                    ToolkitIcons.CenterMap,
                    (s, e) => ShowAssociationOnMap(parent)));
            }

            contextMenu.Items.Add(CreateMenuFlyoutItem(
                Properties.Resources.GetString("FeatureFormUtilityAssociationsMoreInformation"),
                ToolkitIcons.MoreInformation,
                (s, e) => NavigateToAssociationDetails(parent)));

            if (CanRemoveAssociation(parent))
            {
                contextMenu.Items.Add(CreateMenuFlyoutItem(
                    Properties.Resources.GetString("FeatureFormUtilityAssociationsRemoveAssociation"),
                    ToolkitIcons.Delete,
                    (s, e) => _ = RemoveAssociation(AssociationResult?.Association, parent),
                    new SolidColorBrush(Colors.Red)));
            }

            contextMenu.ShowAt(flyoutTarget as FrameworkElement ?? this);
#endif
        }

#if WPF
        private MenuItem CreateMenuItem(string? header, string glyph, RoutedEventHandler clickHandler, Brush? foreground = null)
        {
            TextBlock icon = new TextBlock
            {
                Text = glyph,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = ToolkitIconsFontFamily
            };
            MenuItem item = new MenuItem
            {
                Header = header,
                Icon = icon
            };

            if (foreground is not null)
            {
                icon.Foreground = foreground;
                item.Foreground = foreground;
            }
            item.Click += clickHandler;
            return item;
        }

        private static FontFamily ToolkitIconsFontFamily = new FontFamily(new Uri("pack://application:,,,/"), "Esri.ArcGISRuntime.Toolkit.WPF;component/Assets/toolkit-icons.ttf#calcite-ui-icons-24");

#elif WINDOWS_XAML
        private MenuFlyoutItem CreateMenuFlyoutItem(string? text, string glyph, RoutedEventHandler clickHandler, Brush? foreground = null)
        {
            FontIcon icon = new FontIcon
            {
                Glyph = glyph,
                FontFamily = ToolkitIconsFontFamily
            };
            MenuFlyoutItem item = new MenuFlyoutItem
            {
                Text = text,
                Icon = icon
            };

            if (foreground is not null)
            {
                icon.Foreground = foreground;
                item.Foreground = foreground;
            }

            item.Click += clickHandler;
            return item;
        }

        private static FontFamily ToolkitIconsFontFamily = new FontFamily("ms-appx:///Esri.ArcGISRuntime.Toolkit.WinUI/Assets/toolkit-icons.ttf#calcite-ui-icons-24");
#endif

    }
}
#endif