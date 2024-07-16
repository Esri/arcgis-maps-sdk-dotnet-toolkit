#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class TextFormInputView : Control
    {
        private TextBox? _textInput;
        private TextBlock? _readonlyLabel;
        private ButtonBase? _barcodeButton;


        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_textInput != null)
            {
                _textInput.LostFocus -= TextInput_LostFocus;
                _textInput.TextChanged -= TextInput_TextChanged;
            }
            _textInput = GetTemplateChild("TextInput") as TextBox;
            if (_textInput != null)
            {
                _textInput.LostFocus += TextInput_LostFocus;
                _textInput.TextChanged += TextInput_TextChanged;
            }
            _readonlyLabel = GetTemplateChild("ReadOnlyText") as TextBlock;
            if (_barcodeButton != null)
            {
                _barcodeButton.Click -= BarcodeButton_Click;
            }
            _barcodeButton = GetTemplateChild("BarcodeButton") as ButtonBase;
            if (_barcodeButton != null)
            {
                _barcodeButton.Click += BarcodeButton_Click;
            }
            ConfigureTextBox();
            UpdateValidationState();
        }

        private void BarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Element != null)
                FeatureFormView.GetFeatureFormViewParent(this)?.OnBarcodeButtonClicked(Element);
        }
    }
}
#endif