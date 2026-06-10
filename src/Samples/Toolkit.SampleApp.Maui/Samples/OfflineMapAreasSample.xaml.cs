using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "Jobs", Description = "Demonstrates the OfflineMapAreasView control with preplanned, on-demand, and offline-disabled maps.", ApiKeyRequired = true)]
    public partial class OfflineMapAreasSample : ContentPage
    {
        private const string NapervillePreplanned = "https://www.arcgis.com/home/item.html?id=acc027394bc84c2fb04d1ed317aac674";
        private const string UsBreweries = "https://www.arcgis.com/home/item.html?id=3da658f2492f4cfd8494970ef489d2c5";
        private const string NapervilleOfflineDisabled = "https://www.arcgis.com/home/item.html?id=b95fe18073bc4f7788f0375af2bb445e";

        public OfflineMapAreasSample()
        {
            var vm = new OfflineMapAreasSampleVM()
            {
                Maps = new List<MapAreaViewModel>
                {
                    new() { Name = "Naperville - Preplanned", Map = new Map(new Uri(NapervillePreplanned)) },
                    new() { Name = "US Breweries - On Demand", Map = new Map(new Uri(UsBreweries)) },
                    new() { Name = "Naperville - Offline Disabled", Map = new Map(new Uri(NapervilleOfflineDisabled)) },
                    new() { Name = "No Map", Map = null },
                }
            };
            vm.SelectedMap = vm.Maps.FirstOrDefault();

            BindingContext = vm;

            InitializeComponent();
        }

    }

    public partial class OfflineMapAreasSampleVM : ObservableObject
    {
        public List<MapAreaViewModel> Maps { get; set; }

        [ObservableProperty]
        private MapAreaViewModel? _selectedMap;
    }

    public class MapAreaViewModel
    {
        public string Name { get; set; } = string.Empty;

        public Map? Map { get; set; }

        public override string ToString() => Name;
    }
}
