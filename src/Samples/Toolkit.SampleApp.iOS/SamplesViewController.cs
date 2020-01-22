using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    public partial class SamplesViewController : UITableViewController
    {
        public SamplesViewController(IntPtr handle)
         : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NavigationController.NavigationBar.PrefersLargeTitles = true;

            TableView.Source = new SamplesDataSource(this, SampleDatasource.Current.Samples.ToList());

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
                cell.DetailTextLabel.TextColor = UIColor.SecondaryLabelColor;
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                return cell;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return _data.Count;
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                var item = (Sample) _data[indexPath.Row];
                var control = (UIViewController)Activator.CreateInstance(item.Page);
                _controller.NavigationController.PushViewController(control, true);
            }
        }
    }
}