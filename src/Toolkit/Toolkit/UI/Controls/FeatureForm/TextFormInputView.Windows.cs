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

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class TextFormInputView : Control
    {
            private TextBox? _textInput;


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
            ConfigureTextBox();
            UpdateValidationState();
        }
    }
}
#endif