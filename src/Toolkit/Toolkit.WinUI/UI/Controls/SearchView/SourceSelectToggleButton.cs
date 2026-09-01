using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;

#pragma warning disable CS1591

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public sealed partial class AllSourcesToggleButton : ToggleButton
    {
        private bool _lastIsSelected;

        public AllSourcesToggleButton()
        {
            Checked += AllSourcesToggleButton_CheckedChanged;
            Unchecked += AllSourcesToggleButton_CheckedChanged;
        }

        internal void Invoke() => IsChecked = true;

        private void AllSourcesToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var isSelected = IsChecked == true;
            if (isSelected != _lastIsSelected && FrameworkElementAutomationPeer.FromElement(this) is AllSourcesToggleButtonAutomationPeer peer)
            {
                peer.RaiseSelectionNameChanged(_lastIsSelected, isSelected);
            }

            _lastIsSelected = isSelected;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new AllSourcesToggleButtonAutomationPeer(this);
    }

    internal sealed partial class AllSourcesToggleButtonAutomationPeer : ToggleButtonAutomationPeer, IInvokeProvider
    {
        internal AllSourcesToggleButtonAutomationPeer(AllSourcesToggleButton owner)
            : base(owner)
        {
        }

        public void Invoke() => ((AllSourcesToggleButton)Owner).Invoke();

        internal void RaiseSelectionNameChanged(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                AutomationElementIdentifiers.NameProperty,
                GetAccessibleName(oldValue),
                GetAccessibleName(newValue));
        }

        protected override string GetNameCore() => GetAccessibleName(((ToggleButton)Owner).IsChecked == true);

        protected override object? GetPatternCore(PatternInterface patternInterface)
        {
            return patternInterface switch
            {
                PatternInterface.Invoke => this,
                PatternInterface.Toggle => null,
                _ => base.GetPatternCore(patternInterface),
            };
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        private string GetAccessibleName(bool isSelected)
        {
            var name = base.GetNameCore();
            if (!isSelected)
            {
                return name;
            }

            var format = Properties.Resources.GetString("SearchViewSelectedAutomationName") ?? "{0}, selected";
            return string.Format(CultureInfo.CurrentCulture, format, name);
        }
    }

    public sealed partial class SourceSelectToggleButton : ToggleButton
    {
        private ExpandCollapseState _lastExpandCollapseState = ExpandCollapseState.Collapsed;

        public SourceSelectToggleButton()
        {
            Checked += SourceSelectToggleButton_CheckedChanged;
            Unchecked += SourceSelectToggleButton_CheckedChanged;
        }

        private void SourceSelectToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var newState = GetExpandCollapseState(IsChecked);
            if (newState != _lastExpandCollapseState && FrameworkElementAutomationPeer.FromElement(this) is SourceSelectToggleButtonAutomationPeer peer)
            {
                peer.RaiseExpandCollapseStateChanged(_lastExpandCollapseState, newState);
            }

            _lastExpandCollapseState = newState;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new SourceSelectToggleButtonAutomationPeer(this);

        internal static ExpandCollapseState GetExpandCollapseState(bool? isChecked) => isChecked == true
            ? ExpandCollapseState.Expanded
            : ExpandCollapseState.Collapsed;
    }

    internal sealed partial class SourceSelectToggleButtonAutomationPeer : ToggleButtonAutomationPeer, IExpandCollapseProvider
    {
        internal SourceSelectToggleButtonAutomationPeer(SourceSelectToggleButton owner)
            : base(owner)
        {
        }

        public ExpandCollapseState ExpandCollapseState => SourceSelectToggleButton.GetExpandCollapseState(((ToggleButton)Owner).IsChecked);

        public void Collapse() => ((ToggleButton)Owner).IsChecked = false;

        public void Expand() => ((ToggleButton)Owner).IsChecked = true;

        internal void RaiseExpandCollapseStateChanged(ExpandCollapseState oldState, ExpandCollapseState newState)
        {
            RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldState, newState);
        }

        protected override object? GetPatternCore(PatternInterface patternInterface)
        {
            return patternInterface switch
            {
                PatternInterface.ExpandCollapse => this,
                PatternInterface.Toggle => null,
                _ => base.GetPatternCore(patternInterface),
            };
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
    }
}

#pragma warning restore CS1591