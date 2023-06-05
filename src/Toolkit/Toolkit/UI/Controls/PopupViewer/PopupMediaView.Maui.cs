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

#if MAUI
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.IO;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="Esri.ArcGISRuntime.Mapping.Popups.PopupMedia"/>.
    /// </summary>
    public partial class PopupMediaView : Image
    {
        private ImageSource? Content
        {
            get { return Source; }
            set
            {
                Source = value;
            }
        }

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            if (PopupMedia != null && PopupMedia.Type != PopupMediaType.Image)
            {
                UpdateChart(new Size(widthConstraint, heightConstraint));
            }
            return base.MeasureOverride(widthConstraint, heightConstraint);
        }
    }
}
#endif