using System;
using System.Collections.Generic;
using System.Text;
#if WPF
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
#elif WINUI
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <inheritdoc />
    public partial class ScaleLineAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        private readonly ScaleLine _owner;

        internal ScaleLineAutomationPeer(ScaleLine owner) : base(owner ?? throw new ArgumentNullException(nameof(owner)))
        {
            _owner = owner;
        }
        /// <inheritdoc />
        protected override string GetLocalizedControlTypeCore() => Properties.Resources.GetString("ScaleLineAutomationTypeName")!;
        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        /// <inheritdoc />
        protected override AutomationLiveSetting GetLiveSettingCore() => AutomationLiveSetting.Polite;

        /// <inheritdoc />
#if WPF
        public override object GetPattern(PatternInterface patternInterface)
#elif WINDOWS_XAML
        protected override object GetPatternCore(PatternInterface patternInterface)
#endif
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }
    #if WPF
            return base.GetPattern(patternInterface);
    #elif WINDOWS_XAML
            return base.GetPatternCore(patternInterface);
    #endif
        }
        /// <inheritdoc />
        public string Value => String.Format(Properties.Resources.GetString("ScaleLineAutomationValue")!, GetRoundedValueForAutomationPeer(_owner.MapScale));
        /// <inheritdoc />
        public bool IsReadOnly => true;
        /// <inheritdoc />
        public void SetValue(string value) => throw new System.NotSupportedException("ScaleLine value is read-only.");
        private static double GetRoundedValueForAutomationPeer(double value)
        {
           if (value >= 1000000)
           {
                return value - (value % 1000000);
           }
           else
           {
               return ScaleLine.GetRoundedValue(value);
           }
        }
    }
}
