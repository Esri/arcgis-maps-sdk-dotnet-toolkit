#if WINDOWS && NETCOREAPP || NETFX_CORE
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using Windows.Devices.PointOfService;

namespace Esri.ArcGISRuntime.Toolkit
{
    internal sealed partial class BarcodeScanner : IDisposable
    {
        private ClaimedBarcodeScanner claimedScanner;
        private TaskCompletionSource<string?>? TaskCompletionSource;

        private BarcodeScanner()
        {

        }

        private async Task<string?> ScanAsyncImpl()
        {
            try
            {
                var selector = Windows.Devices.PointOfService.BarcodeScanner.GetDeviceSelector();
                var deviceCollection = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(selector);
                Windows.Devices.PointOfService.BarcodeScanner? scanner = null;
                foreach (var device in deviceCollection)
                {
                    var candidate = await Windows.Devices.PointOfService.BarcodeScanner.FromIdAsync(device.Id);
                    if(candidate.VideoDeviceId is not null)
                    {
                        scanner = candidate;
                        continue;
                    }
                    var videoDevice = await Windows.Devices.Enumeration.DeviceInformation.CreateFromIdAsync(candidate.VideoDeviceId);
                    if (videoDevice.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Back) // prefer back panel camera
                        scanner = candidate;
                }
                scanner = scanner ?? await Windows.Devices.PointOfService.BarcodeScanner.GetDefaultAsync();
                if (scanner is null || scanner.VideoDeviceId is null)
                    throw new NotSupportedException();
                
                claimedScanner = await scanner.ClaimScannerAsync();
                 claimedScanner.IsDisabledOnDataReceived = true;
                claimedScanner.DataReceived += ClaimedScanner_DataReceived;
                claimedScanner.Closed += ClaimedScanner_Closed;
                await claimedScanner.EnableAsync();
                if (scanner.Capabilities.IsSoftwareTriggerSupported)
                    await claimedScanner.StartSoftwareTriggerAsync();
                await claimedScanner.ShowVideoPreviewAsync();
                TaskCompletionSource = new TaskCompletionSource<string?>();
                return await TaskCompletionSource.Task;
            }
            catch(System.Exception ex)
            {
                TaskCompletionSource?.TrySetException(ex);
                throw;
            }
            finally
            {
                await claimedScanner.DisableAsync();
            }
        }

        private void ClaimedScanner_Closed(ClaimedBarcodeScanner sender, ClaimedBarcodeScannerClosedEventArgs args)
        {
            TaskCompletionSource?.TrySetCanceled();
        }

        public static async Task<string?> ScanAsync()
        {
            using var scanner = new BarcodeScanner();
            var result = await scanner.ScanAsyncImpl();
            return result;
        }

        private void ClaimedScanner_DataReceived(ClaimedBarcodeScanner sender, BarcodeScannerDataReceivedEventArgs args)
        {
            TaskCompletionSource?.TrySetResult(args.Report.ScanDataLabel.ToString());
        }

        public void Dispose()
        {
            claimedScanner?.Dispose();
        }
    }
}
#endif