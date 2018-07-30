﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.PopupViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PopupViewerSample : Page
    {
        public PopupViewerSample()
        {
            this.InitializeComponent();
            
        }

        public Map Map { get; } = new Map(new Uri("https://www.arcgis.com/home/item.html?id=d4fe39d300c24672b1821fa8450b6ae2"));


        private async void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            Exception error = null;
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);
                var popup = GetPopup(result);
                if (popup != null)
                {
                    if (popup.PopupDefinition != null && !popup.PopupDefinition.ShowAttachments)
                        popup.PopupDefinition.ShowEditSummary = true;
                    popupViewer.PopupManager = new PopupManager(popup);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            if (error != null)
                await new MessageDialog(error.Message, error.GetType().Name).ShowAsync();
        }

        private Popup GetPopup(IdentifyLayerResult result)
        {
            if (result == null)
            {
                return null;
            }

            var popup = result.Popups.FirstOrDefault();
            if (popup != null)
            {
                return popup;
            }

            var geoElement = result.GeoElements.FirstOrDefault();
            if (geoElement != null)
            {
                if (result.LayerContent is IPopupSource)
                {
                    var popupDefinition = ((IPopupSource)result.LayerContent).PopupDefinition;
                    if (popupDefinition != null)
                    {
                        return new Popup(geoElement, popupDefinition);
                    }
                }

                return Popup.FromGeoElement(geoElement);
            }

            return null;
        }

        private Popup GetPopup(IEnumerable<IdentifyLayerResult> results)
        {
            if (results == null)
            {
                return null;
            }
            foreach (var result in results)
            {
                var popup = GetPopup(result);
                if (popup != null)
                {
                    return popup;
                }

                foreach (var subResult in result.SublayerResults)
                {
                    popup = GetPopup(subResult);
                    if (popup != null)
                    {
                        return popup;
                    }
                }
            }

            return null;
        }
    }
}