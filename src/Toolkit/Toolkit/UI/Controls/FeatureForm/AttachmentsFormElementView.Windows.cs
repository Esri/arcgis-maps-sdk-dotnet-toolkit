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
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name = "AddAttachmentButton", Type = typeof(ButtonBase))]
    public partial class AttachmentsFormElementView : Control
    {
        private ButtonBase? _addAttachmentButton;
        private bool _scrollToEnd;

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (_addAttachmentButton is not null)
            {
                _addAttachmentButton.Click -= AddAttachmentButton_Click;
            }
            _addAttachmentButton = GetTemplateChild("AddAttachmentButton") as ButtonBase;
            if(_addAttachmentButton is not null)
            {
                _addAttachmentButton.Click += AddAttachmentButton_Click;
            }
            if(GetTemplateChild("ItemsScrollView") is ScrollViewer scrollViewer)
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            UpdateVisibility();
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(_scrollToEnd)
            {
                (sender as ScrollViewer)?.ScrollToRightEnd();
                _scrollToEnd = false;
            }
        }

        private void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (Element is null || !Element.IsEditable) return;
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Exists)
                    {
                        _scrollToEnd = true;
                        Element.AddAttachment(fileInfo.Name, MimeTypeMap.GetMimeType(fileInfo.Extension), File.ReadAllBytes(fileInfo.FullName));
                        EvaluateExpressions();
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Failed to add attachment: " + ex.Message);
            }
        }
    }
}
#endif
