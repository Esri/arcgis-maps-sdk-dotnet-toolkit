#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks.Offline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Offline
{
    // [SampleInfo(ApiKeyRequired = true)]
    public partial class OfflineMapAreasSample : UserControl
    {
        public OfflineMapAreasSample()
        {
            InitializeComponent();
            this.DataContext = new OfflineMapAreasVM();
        }
    }

    public partial class OfflineMapAreasVM : ObservableObject
    {
        public OfflineMapAreasVM()
        {
            Init();    
        }

        private async void Init()
        {
            Naperville = await PortalItem.CreateAsync(await ArcGISPortal.CreateAsync(), "acc027394bc84c2fb04d1ed317aac674");
        }

        [ObservableProperty]
        private Map? _onlineMap;

        [ObservableProperty]
        private PortalItem? _naperville;

        partial void OnNapervilleChanged(PortalItem? value)
        {
            if (value is not null)
            {
                OnlineMap = new Map(value);
                if (!IsOffline)
                    SelectedMap = OnlineMap;
            }
            else
                OnlineMap = null;
        }

        [ObservableProperty]
        private Map? _selectedMap;

        [ObservableProperty]
        private bool _isOfflineAvailable;

        [ObservableProperty]
        private bool _isOffline;

        partial void OnIsOfflineChanged(bool value)
        {
            if (value)
                SelectedMap = SelectedOfflineMap;
            else
                SelectedMap = OnlineMap;
        }

        [ObservableProperty]
        private Map? _selectedOfflineMap;

        partial void OnSelectedOfflineMapChanged(Map value)
        {
            IsOfflineAvailable = value != null;
        }
    }
}
