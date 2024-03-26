#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.ComponentModel;

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

        private static object BuildDefaultTemplate()
        {
            Switch view = new Switch();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(view, nameScope);
            nameScope.RegisterName(SwitchViewName, view);
            return view;
        }
        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if(GetTemplateChild(SwitchViewName) is Switch switchView)
            {
#if WINDOWS
                switchView.HandlerChanged += SwitchView_HandlerChanged;
#endif
                switchView.Toggled += SwitchView_Toggled;
                UpdateEditableState();
            }
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
                // Note: ios/android does not have on/off content placeholders
            }
        }
#endif

        private void SwitchView_Toggled(object? sender, ToggledEventArgs e)
        {
            if(e.Value)
                Checked();
            else 
                Unchecked();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this switch is checked or not.
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FeatureForm"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsCheckedProperty =
            BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(SwitchFormInputView), propertyChanged: (s, oldValue, newValue) => ((SwitchFormInputView)s).OnIsCheckedPropertyChanged(oldValue, newValue));

        private void OnIsCheckedPropertyChanged(object oldValue, object newValue)
        {
            if(newValue is bool b && b)
                Checked();
            else
                Unchecked();
        }
    }
}
#endif