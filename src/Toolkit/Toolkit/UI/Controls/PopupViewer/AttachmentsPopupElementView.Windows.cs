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

#if WPF
using Esri.ArcGISRuntime.Mapping.Popups;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;

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

        /// <summary>
        /// Occurs when an attachment is clicked.
        /// </summary>
        /// <remarks>Override this to prevent the default "save to file dialog" action.</remarks>
        /// <param name="attachment">Attachment clicked.</param>
        public virtual async void OnAttachmentClicked(PopupAttachment attachment)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = attachment.Name;
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using var stream = await attachment.Attachment!.GetDataAsync();
                    using var outfile = saveFileDialog.OpenFile();
                    await stream.CopyToAsync(outfile);
                }
                catch { }
            }
        }
    }
}
#endif