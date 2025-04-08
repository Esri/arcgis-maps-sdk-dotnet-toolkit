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
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
#if WPF
using System.Windows.Controls.Primitives;
using System.Windows.Input;
#endif
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class TextFormInputView : Control
    {
        private TextBox? _textInput;
        private TextBlock? _readonlyLabel;
        private ButtonBase? _barcodeButton;


        /// <inheritdoc />
#if WPF
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
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
            {
                bool handled = FeatureFormView.GetFeatureFormViewParent(this)?.OnBarcodeButtonClicked(Element) == true;
                if(!handled)
                {
                    LaunchBarcodeScanner(Element);
                }
            }
        }
    }
}
#endif