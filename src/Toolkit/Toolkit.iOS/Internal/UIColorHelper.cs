using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit
{
    internal static class UIColorHelper
    {
        private static readonly bool _isIOS13OrNewer;

        static UIColorHelper()
        {
            _isIOS13OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(13, 0);
        }

        public static UIColor LabelColor => _isIOS13OrNewer ? UIColor.LabelColor : UIColor.Black;

        public static UIColor SystemBackgroundColor => _isIOS13OrNewer ? UIColor.SystemBackgroundColor : UIColor.White;
    }
}
