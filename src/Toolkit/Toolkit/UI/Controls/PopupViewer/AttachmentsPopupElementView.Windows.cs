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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using System.IO;

#if NET6_0_OR_GREATER
using System.Runtime.InteropServices.WindowsRuntime;

#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="AttachmentsPopupElement"/>.
    /// </summary>
    [TemplatePart(Name = AttachmentListName, Type = typeof(ListBox))]
    public partial class AttachmentsPopupElementView : Control
    {
        private const string AttachmentListName = "AttachmentList";

        private UI.Controls.PopupViewer? GetPopupViewerParent()
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent is not null && parent is not UI.Controls.PopupViewer popup)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as UI.Controls.PopupViewer;
        }
    }
}
#endif
