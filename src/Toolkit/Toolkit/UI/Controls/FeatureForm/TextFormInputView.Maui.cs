#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Text input for the <see cref="TextAreaFormInput"/> and <see cref="TextBoxFormInput"/> inputs.
    /// </summary>
    public partial class TextFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private InputView? _textInput => Element?.Input is TextAreaFormInput ? _textAreaInput : _textLineInput;
        private Entry? _textLineInput;
        private Editor? _textAreaInput;
        static TextFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement.IsEditable), "Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            
            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            HorizontalStackLayout horizontalStackLayout = new HorizontalStackLayout();
            horizontalStackLayout.Margin = new Thickness(0, -17, 0, 0);
            horizontalStackLayout.SetBinding(View.IsVisibleProperty, new Binding(nameof(TextFormInputView.ShowCharacterCount), source: RelativeBindingSource.TemplatedParent));
            Label characterCountLabel = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle() };
            characterCountLabel.SetBinding(Label.TextProperty, new Binding("Element.Value.Length", source: RelativeBindingSource.TemplatedParent));
            horizontalStackLayout.Children.Add(characterCountLabel);
            horizontalStackLayout.Children.Add(new Label() { Text = "/", Style = FeatureFormView.GetFeatureFormCaptionStyle() });
            Label maxCountLabel = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle() };
            maxCountLabel.SetBinding(Label.TextProperty, new Binding("Element.Input.MaxLength", source: RelativeBindingSource.TemplatedParent));
            horizontalStackLayout.Children.Add(maxCountLabel);
            Grid.SetColumn(horizontalStackLayout, 1);
            root.Add(horizontalStackLayout);
            Entry textInput = new Entry();
            Grid.SetColumnSpan(textInput, 2);
            root.Add(textInput);
            textInput.SetBinding(Entry.IsEnabledProperty, "Element.IsEditable");
            Editor textArea = new Editor() { IsVisible = false, HeightRequest = 100, AutoSize = EditorAutoSizeOption.Disabled };
            Grid.SetColumnSpan(textArea, 2);
            textArea.SetBinding(Editor.IsEnabledProperty, "Element.IsEditable");
            root.Add(textArea);
            
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("TextInput", textInput);
            nameScope.RegisterName("TextAreaInput", textArea);
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
            ConfigureTextBox();
            UpdateValidationState();
        }
    }
}
#endif