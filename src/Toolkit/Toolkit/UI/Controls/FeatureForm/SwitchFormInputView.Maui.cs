#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class SwitchFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static SwitchFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        /// <summary>
        /// Template name of the <see cref="Switch"/> view control.
        /// </summary>
        public const string SwitchViewName = "SwitchView";

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement.Input), "Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.SwitchFormInput.OnValue), "Esri.ArcGISRuntime.Mapping.FeatureForms.SwitchFormInput", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.SwitchFormInput.OffValue), "Esri.ArcGISRuntime.Mapping.FeatureForms.SwitchFormInput", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Data.CodedValue.Name), "Esri.ArcGISRuntime.Data.CodedValue", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            Switch toggleSwitch = new Switch();
            toggleSwitch.SetBinding(Switch.IsToggledProperty, new Binding(nameof(IsChecked), source: RelativeBindingSource.TemplatedParent));
            INameScope nameScope = new NameScope();
            nameScope.RegisterName(SwitchViewName, toggleSwitch);
#if WINDOWS
            NameScope.SetNameScope(toggleSwitch, nameScope);
            return toggleSwitch;
#else
            toggleSwitch.VerticalOptions = new LayoutOptions(LayoutAlignment.Center, false);
            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            Grid.SetColumn(toggleSwitch, 1);
            Label onText = new Label() { VerticalOptions = new LayoutOptions(LayoutAlignment.Center, false) };
            onText.SetBinding(Label.TextProperty, new Binding("Element.Input.OnValue.Name", source: RelativeBindingSource.TemplatedParent));
            onText.SetBinding(Label.IsVisibleProperty, new Binding(nameof(IsChecked), source: RelativeBindingSource.TemplatedParent));
            root.Children.Add(onText);

            Label offText = new Label() { VerticalOptions = new LayoutOptions(LayoutAlignment.Center, false) };
            offText.SetBinding(Label.TextProperty, new Binding("Element.Input.OffValue.Name", source: RelativeBindingSource.TemplatedParent));
            offText.SetBinding(Label.IsVisibleProperty, new Binding(nameof(IsChecked), source: RelativeBindingSource.TemplatedParent, converter: Internal.InvertBoolConverter.Instance));
            root.Children.Add(offText);

            root.Children.Add(toggleSwitch);
            NameScope.SetNameScope(root, nameScope);
            return root;
#endif
        }
        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
#if WINDOWS
            if(GetTemplateChild(SwitchViewName) is Switch switchView)
            {
                switchView.HandlerChanged += SwitchView_HandlerChanged;
            }
#endif
            UpdateEditableState();
        }

        private void UpdateEditableState()
        {
            if (GetTemplateChild(SwitchViewName) is Switch switchView)
            {
                switchView.IsEnabled = Element?.IsEditable == true;
            }
        }

#if WINDOWS
        private void SwitchView_HandlerChanged(object? sender, EventArgs e)
        {
            UpdateOnOffContent();
        }

        private void UpdateOnOffContent()
        {
            if (GetTemplateChild(SwitchViewName) is Switch view
                && view.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ToggleSwitch ts)
            {
                ts.OnContent = (Element?.Input as SwitchFormInput)?.OnValue?.Name;
                ts.OffContent = (Element?.Input as SwitchFormInput)?.OffValue?.Name;
                // Note: ios/android does not have built-in on/off content placeholders, and is handled with text content instead - see BuildDefaultTemplate
            }
        }
#endif
    }
}
#endif