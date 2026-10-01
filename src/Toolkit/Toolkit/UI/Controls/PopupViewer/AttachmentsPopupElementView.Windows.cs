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
#if WPF
using System.Windows.Automation.Peers;
#endif

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

#if WPF
        /// <summary>
        /// Creates a new instance of the <see cref="AttachmentsPopupElementViewPeer"/> class.
        /// </summary>
        /// <returns></returns>
        protected override AutomationPeer OnCreateAutomationPeer() => new AttachmentsPopupElementViewPeer(this);
#endif

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

#if WPF
    /// <summary>
    /// Automation peer for the <see cref="AttachmentsPopupElementView"/> control.
    /// </summary>
    public class AttachmentsPopupElementViewPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentsPopupElementViewPeer"/> class.
        /// </summary>
        /// <param name="owner"></param>
        public AttachmentsPopupElementViewPeer(AttachmentsPopupElementView owner) : base(owner)
        {
        }

        /// <summary>
        /// Indicates that this control is not a content element for accessibility purposes.
        /// </summary>
        /// <returns></returns>
        protected override bool IsContentElementCore()
        {
            return false;
        }

        /// <summary>
        /// Indicates that this control is not a content element for accessibility purposes.
        /// </summary>
        /// <returns></returns>
        protected override bool IsControlElementCore()
        {
            return false;
        }
    }
#endif
}
#endif
