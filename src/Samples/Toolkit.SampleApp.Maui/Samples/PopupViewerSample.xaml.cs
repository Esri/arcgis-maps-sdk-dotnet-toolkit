using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Maui;
using System.Diagnostics;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "PopupViewer", Description = "Use PopupViewer to display detailed feature information")]
    public partial class PopupViewerSample : ContentPage
    {
        public PopupViewerSample()
        {
            InitializeComponent();
            mapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad"));
            mapView.GeoViewTapped += mapView_GeoViewTapped;
            this.BindingContext = VM = new ViewModel();
        }

        private void CloseButton_Clicked(object sender, EventArgs e)
        {
            VM.Popups = null;
        }

        public ViewModel VM { get; }


        private async void mapView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            Exception? error = null;
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 2, false, 10);
                var popups = GetGeoElements(result).Select(e => new Popup(e, null));
                VM.Popups = popups.ToList();
            }
            catch (Exception ex)
            {
                error = ex;

            }
            if (error != null)
                await DisplayAlert(error.GetType().Name, error.Message, "OK");
        }
        public IEnumerable<GeoElement> GetGeoElements(IEnumerable<IdentifyLayerResult> results)
        {
            foreach (var result in results)
            {
                foreach (var elm in result.GeoElements)
                    yield return elm;
                foreach (var item in GetGeoElements(result.SublayerResults))
                    yield return item;
            }
        }


        private void popupViewer_PopupAttachmentClicked(object sender, Esri.ArcGISRuntime.Toolkit.Maui.PopupAttachmentClickedEventArgs e)
        {
            e.Handled = true; // Prevent default launch action
            // Share file:
            // _ = Share.Default.RequestAsync(new ShareFileRequest(new ReadOnlyFile(e.Attachment.Filename!, e.Attachment.ContentType)));

            // Open default file handler
            _ = Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(
                 new Microsoft.Maui.ApplicationModel.OpenFileRequest(e.Attachment.Name, new ReadOnlyFile(e.Attachment.Filename!, e.Attachment.ContentType)));
        }

        private void popupViewer_HyperlinkClicked(object sender, Esri.ArcGISRuntime.Toolkit.Maui.HyperlinkClickedEventArgs e)
        {
            // Include below line if you want to prevent the default action
            // e.Handled = true;

            // Perform custom action when a link is clicked
            Debug.WriteLine(e.Uri);
        }
    }

    public partial class ViewModel : ObservableObject
    {
        public bool IsSidePanelOpen => Popups?.Count > 0;

        public bool HasMoreThanOneResult => Popups?.Count > 1;

        [ObservableProperty]
        private IList<Popup>? _popups;

        async partial void OnPopupsChanged(IList<Popup>? value)
        {
            OnPropertyChanged(nameof(IsSidePanelOpen));
            OnPropertyChanged(nameof(HasMoreThanOneResult));
            await Task.Yield(); // Allows picker to refresh
            if (SelectedPopup is null || value?.Contains(SelectedPopup) != true)
                SelectedPopup = value?.FirstOrDefault();
        }

        [ObservableProperty]
        private Popup? _selectedPopup;

        partial void OnSelectedPopupChanged(Popup? oldValue, Popup? newValue)
        {
            if (oldValue is not null)
            {
                if (oldValue.GeoElement is Feature feature && feature.FeatureTable?.Layer is FeatureLayer featureLayer)
                    featureLayer.UnselectFeature(feature);
            }
            if (newValue is not null)
            {
                if (newValue.GeoElement is Feature feature && feature.FeatureTable?.Layer is FeatureLayer featureLayer)
                    featureLayer.SelectFeature(feature);
            }
        }
    }
}