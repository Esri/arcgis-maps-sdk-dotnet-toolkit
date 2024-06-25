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
        }

        private void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (Element is null) return;
            // TODO: Allow overriding what happens here
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var fileInfo = new FileInfo(openFileDialog.FileName);
                if (fileInfo.Exists)
                {
                    Element.AddAttachment(fileInfo.Name, MimeTypeMap.GetMimeType(fileInfo.Extension), File.ReadAllBytes(fileInfo.FullName));
                }
            }
        }
    }
}
#endif