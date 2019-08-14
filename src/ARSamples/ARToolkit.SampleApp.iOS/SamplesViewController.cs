using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

namespace ARToolkit.SampleApp
{
    public partial class SamplesViewController : UITableViewController
    {
        private static SampleDatasource list = new SampleDatasource();

        public SamplesViewController(IntPtr handle)
         : base(handle)
        {
        }


        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NavigationController.NavigationBar.PrefersLargeTitles = true;

            TableView.Source = new SamplesDataSource(this, list.Samples);

            TableView.ReloadData();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            NavigationController.NavigationBar.PrefersLargeTitles = false;
        }

        private class SamplesDataSource : UITableViewSource
        {
            private readonly UITableViewController _controller;
            private readonly IList<Sample> _data;

            public SamplesDataSource(UITableViewController controller, IList<Sample> data)
            {
                _data = data;
                _controller = controller;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var cell = tableView.DequeueReusableCell("sample") ?? new UITableViewCell(UITableViewCellStyle.Subtitle, "sample");
                Sample item = _data[indexPath.Row];
                cell.TextLabel.Text = item.Name;
                cell.DetailTextLabel.Text = item.Description;
                cell.DetailTextLabel.TextColor = UIColor.Gray;
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                return cell;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return _data.Count;
            }

            public override async void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                var item = _data[indexPath.Row];

                if(!item.IsDeviceSupported)
                {
                    UIAlertView _error = new UIAlertView("Not supported", "This device does not support running this sample.", del: null, "Ok");
                    _error.Show();
                    return;
                }
                if (item.HasSampleData)
                {
                    // Show progress overlay
                    var bounds = UIScreen.MainScreen.Bounds;
                    var _loadPopup = new LoadingOverlay(bounds);
                    _controller.ParentViewController.View.Add(_loadPopup);
                    try
                    {
                        await item.GetDataAsync((status) =>
                        {
                            InvokeOnMainThread(() => _loadPopup.SetText(status));
                        });
                    }
                    catch (System.Exception ex)
                    {
                        UIAlertView _error = new UIAlertView("Failed to download data", ex.Message, null, "Ok");
                        _error.Show();
                        return;
                    }
                    finally
                    {
                        _loadPopup.Hide();
                    }
                }
                var control = (UIViewController)Activator.CreateInstance(item.Type);
                _controller.NavigationController.PushViewController(control, true);
            }
        }
    }
}