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

using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Text;
using Esri.ArcGISRuntime.Data;

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
using TextBlock = Microsoft.Maui.Controls.Label;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Supporting control for the <see cref="FeatureFormView"/> control,
    /// used for rendering a <see cref="UtilityAssociationResult"/>.
    /// </summary>
    public partial class UtilityAssociationResultDetailsView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationResultDetailsView"/> class.
        /// </summary>
        public UtilityAssociationResultDetailsView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
            this.ParentChanged += (s, e) => UpdateView();
#else
            DefaultStyleKey = typeof(UtilityAssociationResultDetailsView);
#endif
        }

        /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("RemoveAssociationButton") is Button button)
            {
#if MAUI
                button.Clicked += (s, e) => RemoveAssociation();
#else
                button.Click += (s, e) => RemoveAssociation();
#endif
            }
            UpdateView();
        }

        private async void RemoveAssociation()
        {
            if (AssociationResult?.Association is null || !await ConfirmDeleteAssociationAsync())
            {
                return;
            }

            // If the page navigated back to no longer has associations, then navigate one more page back.
            var formview = FeatureFormView.GetFeatureFormViewParent(this);
            var form = formview?.CurrentFeatureForm;            
            var result = formview?.GetNavigationStack().OfType<UtilityAssociationsFilterResult>().LastOrDefault();
            var a = form?.Elements.OfType<UtilityAssociationsFormElement>().Where(e => e.AssociationsFilterResults.Contains(result))?.FirstOrDefault();
            if (a is null || !a.IsEditable) return;  // TODO: we shouldn't show remove if it can't be edited

            try
            {
                
                a.DeleteAssociation(AssociationResult.Association);
            }
            catch
            {
                return; // TODO:...
            }
            await a.FetchAssociationsFilterResultsAsync();
            //TODO: Refreshing it will replace all the collections, and the backstack will contain stale versions of the collections.
            var previousPage = await (formview?.GoBackAsync());
            // if(previousPage is )
        }

        /// <summary>
        /// Gets or sets the AssociationResult.
        /// </summary>
        public UtilityAssociationResult? AssociationResult
        {
            get => GetValue(AssociationResultProperty) as UtilityAssociationResult;
            set => SetValue(AssociationResultProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AssociationResult"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AssociationResultProperty =
            PropertyHelper.CreateProperty<UtilityAssociationResult, UtilityAssociationResultDetailsView>(nameof(AssociationResult), null, (s, oldValue, newValue) => s.OnAssociationResultPropertyChanged());

        private void OnAssociationResultPropertyChanged()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            var title = GetTemplateChild("Title") as TextBlock;
            var ffv = FeatureFormView.GetFeatureFormViewParent(this);

            if (GetTemplateChild("RemoveAssociationButton") is Button button)
            {
                var form = ffv?.CurrentFeatureForm;
                var result = ffv?.GetNavigationStack().OfType<UtilityAssociationsFilterResult>().LastOrDefault();
                var a = form?.Elements.OfType<UtilityAssociationsFormElement>().Where(e => e.AssociationsFilterResults.Contains(result))?.FirstOrDefault();
#if MAUI
                button.IsVisible = a?.IsEditable == true;
#else
                button.Visibility = a?.IsEditable == true ? Visibility.Visible : Visibility.Collapsed;
#endif
            }

            if (GetTemplateChild("FromElementText") is TextBlock fromElementText)
            {
                fromElementText.Text = ffv?.CurrentFeatureForm?.Title;
            }

            if (GetTemplateChild("ToElementText") is TextBlock toElementText)
            {
                toElementText.Text = AssociationResult?.AssociatedFeature is null ? "" : new FeatureForm(AssociationResult?.AssociatedFeature!)?.Title;
            }

            if (GetTemplateChild("FromTerminalText") is TextBlock fromTerminalText)
            {
                fromTerminalText.Text = AssociationResult?.Association?.FromElement?.Terminal?.Name;
#if MAUI
                fromTerminalText.IsVisible = !string.IsNullOrEmpty(fromTerminalText.Text);
#else
                fromTerminalText.Visibility = string.IsNullOrEmpty(fromTerminalText.Text) ? Visibility.Collapsed : Visibility.Visible;
#endif
            }

            if (GetTemplateChild("ToTerminalText") is TextBlock toTerminalText)
            {
                toTerminalText.Text = AssociationResult?.Association?.ToElement?.Terminal?.Name;
#if MAUI
                toTerminalText.IsVisible = !string.IsNullOrEmpty(toTerminalText.Text);
#else
                toTerminalText.Visibility = string.IsNullOrEmpty(toTerminalText.Text) ? Visibility.Collapsed : Visibility.Visible;
#endif
            }
        }

        private async System.Threading.Tasks.Task<bool> ConfirmDeleteAssociationAsync()
        {
            string title = Esri.ArcGISRuntime.Toolkit.Properties.Resources.GetString("FeatureFormDeleteAssociationConfirmationTitle")!;
            string message = Esri.ArcGISRuntime.Toolkit.Properties.Resources.GetString("FeatureFormDeleteAssociationConfirmationMessage")!;
            string accept = Esri.ArcGISRuntime.Toolkit.Properties.Resources.GetString("FeatureFormDeleteAssociationConfirmationAccept")!;
            string cancel = Esri.ArcGISRuntime.Toolkit.Properties.Resources.GetString("FeatureFormDeleteAssociationConfirmationCancel")!;
#if WPF
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                System.Windows.Window.GetWindow(this),
                message,
                title,
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.Cancel);
            return result == System.Windows.MessageBoxResult.Yes;
#elif WINDOWS_XAML
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = title,
                Content = message,
                PrimaryButtonText = accept,
                CloseButtonText = cancel,
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
            };
            return await dialog.ShowAsync() == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary;
#elif MAUI
            Microsoft.Maui.Controls.Page? page = Window?.Page;

            if (page is null && Microsoft.Maui.Controls.Application.Current is not null)
            {
                foreach (Microsoft.Maui.Controls.Window window in Microsoft.Maui.Controls.Application.Current.Windows)
                {
                    if (window.Page is not null)
                    {
                        page = window.Page;
                        break;
                    }
                }
            }

            return page is not null
                && await page.DisplayAlert(title, message, accept, cancel);
#endif
        }
    }
}