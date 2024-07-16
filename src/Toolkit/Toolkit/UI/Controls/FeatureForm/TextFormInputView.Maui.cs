#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class TextFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private InputView? _textInput => Element?.Input is TextAreaFormInput ? _textAreaInput : _textLineInput;
        private Entry? _textLineInput;
        private Editor? _textAreaInput;
        private Label? _readonlyLabel;
        static TextFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private class StringLengthConverter : IValueConverter
        {
            public static StringLengthConverter Instance { get; } = new StringLengthConverter();
            object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is string str ? str.Length : 0;

            object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement.IsEditable), "Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement.Value), "Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.TextAreaFormInput.MaxLength), "Esri.ArcGISRuntime.Mapping.FeatureForms.TextAreaFormInput", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.TextBoxFormInput.MaxLength), "Esri.ArcGISRuntime.Mapping.FeatureForms.TextBoxFormInput", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.BarcodeScannerFormInput.MaxLength), "Esri.ArcGISRuntime.Mapping.FeatureForms.BarcodeScannerFormInput", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            
            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            HorizontalStackLayout horizontalStackLayout = new HorizontalStackLayout();
            horizontalStackLayout.Margin = new Thickness(0, -17, 0, 0);
            horizontalStackLayout.SetBinding(View.IsVisibleProperty, new Binding(nameof(TextFormInputView.ShowCharacterCount), source: RelativeBindingSource.TemplatedParent));
            Label characterCountLabel = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle() };
            characterCountLabel.SetBinding(Label.TextProperty, new Binding("Element.Value", source: RelativeBindingSource.TemplatedParent, converter: StringLengthConverter.Instance));
            horizontalStackLayout.Children.Add(characterCountLabel);
            horizontalStackLayout.Children.Add(new Label() { Text = "/", Style = FeatureFormView.GetFeatureFormCaptionStyle() });
            Label maxCountLabel = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle() };
            maxCountLabel.SetBinding(Label.TextProperty, new Binding("Element.Input.MaxLength", source: RelativeBindingSource.TemplatedParent));
            horizontalStackLayout.Children.Add(maxCountLabel);
            Grid.SetColumn(horizontalStackLayout, 1);
            Grid.SetColumnSpan(horizontalStackLayout, 2);
            root.Add(horizontalStackLayout);
            Entry textInput = new Entry();
            Grid.SetColumnSpan(textInput, 2);
            root.Add(textInput);
            textInput.SetBinding(Entry.IsEnabledProperty, "Element.IsEditable");
            Editor textArea = new Editor() { IsVisible = false, HeightRequest = 100, AutoSize = EditorAutoSizeOption.Disabled };
            Grid.SetColumnSpan(textArea, 2);
            textArea.SetBinding(Editor.IsEnabledProperty, "Element.IsEditable");
            root.Add(textArea);
            Label readonlyText = new Label() { IsVisible = false, LineBreakMode = LineBreakMode.WordWrap };
            readonlyText.SetBinding(Label.TextProperty, new Binding("Element.Value",source:RelativeBindingSource.TemplatedParent));
            Grid.SetColumnSpan(readonlyText, 2);
            root.Add(readonlyText);
            Border errorBorder = new Border() { StrokeThickness = 1, Stroke = new SolidColorBrush(Colors.Red), IsVisible = false };
            Grid.SetColumnSpan(errorBorder, 2);
            root.Add(errorBorder);
            Internal.CalciteImageButton barcodeButton = new Internal.CalciteImageButton("\uE22F") { IsVisible = false, BorderWidth = 0 };
            Grid.SetColumn(barcodeButton, 2);
            barcodeButton.SetBinding(View.IsVisibleProperty, new Binding(nameof(TextFormInputView.ShowBarcodeScanner), source: RelativeBindingSource.TemplatedParent));
            root.Add(barcodeButton);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("ErrorBorder", errorBorder);
            nameScope.RegisterName("TextInput", textInput);
            nameScope.RegisterName("TextAreaInput", textArea);
            nameScope.RegisterName("ReadOnlyText", readonlyText);
            nameScope.RegisterName("BarcodeButton", barcodeButton);
            return root;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_textLineInput != null)
            {
                _textLineInput.Unfocused -= TextInput_Unfocused;
                _textLineInput.TextChanged -= TextInput_TextChanged;
            }
            if (_textAreaInput != null)
            {
                _textAreaInput.Unfocused -= TextInput_Unfocused;
                _textAreaInput.TextChanged -= TextInput_TextChanged;
            }
            _textLineInput = GetTemplateChild("TextInput") as Entry;
            if (_textLineInput != null)
            {
                _textLineInput.Unfocused += TextInput_Unfocused;
                _textLineInput.TextChanged += TextInput_TextChanged;
            }
            _textAreaInput = GetTemplateChild("TextAreaInput") as Editor;
            if (_textAreaInput != null)
            {
                _textAreaInput.Unfocused += TextInput_Unfocused;
                _textAreaInput.TextChanged += TextInput_TextChanged;
            }
            _readonlyLabel = GetTemplateChild("ReadOnlyText") as Label;
            if (GetTemplateChild("BarcodeButton") is ImageButton imageButton)
            {
                imageButton.Clicked += BarcodeButton_Clicked;
            }
            else if (GetTemplateChild("BarcodeButton") is Button button)
            {
                button.Clicked += BarcodeButton_Clicked;
            }
            ConfigureTextBox();
            UpdateValidationState();
        }

        private void BarcodeButton_Clicked(object? sender, EventArgs e)
        {
            if (Element != null)
                FeatureFormView.GetFeatureFormViewParent(this)?.OnBarcodeButtonClicked(Element);
        }
    }
}
#endif