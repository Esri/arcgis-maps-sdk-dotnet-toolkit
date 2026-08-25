using System.Globalization;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;

#pragma warning disable CS1591

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    // Keeps ToggleButton styling/binding from XAML while reporting button-style automation behavior.
    public sealed class AllSourcesToggleButton : ToggleButton
    {
        // We cache the prior state because Checked/Unchecked handlers only expose the new state.
        private bool _lastIsSelected;

        public AllSourcesToggleButton()
        {
            Checked += AllSourcesToggleButton_CheckedChanged;
            Unchecked += AllSourcesToggleButton_CheckedChanged;
        }

        internal void Invoke() => OnClick();

        private void AllSourcesToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var isSelected = IsChecked == true;
            if (isSelected != _lastIsSelected && UIElementAutomationPeer.FromElement(this) is AllSourcesToggleButtonAutomationPeer peer)
            {
                peer.RaiseSelectionNameChanged(_lastIsSelected, isSelected);
            }

            _lastIsSelected = isSelected;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new AllSourcesToggleButtonAutomationPeer(this);
    }

    internal sealed class AllSourcesToggleButtonAutomationPeer : ToggleButtonAutomationPeer, IInvokeProvider
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

        public override object? GetPattern(PatternInterface patternInterface)
        {
            // Expose Invoke and hide Toggle so Narrator does not read the control as a toggle switch.
            return patternInterface switch
            {
                PatternInterface.Invoke => this,
                PatternInterface.Toggle => null,
                _ => base.GetPattern(patternInterface),
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

            // Appending "selected" to the Name property produces consistent speech across screen readers.
            var format = Properties.Resources.GetString("SearchViewSelectedAutomationName") ?? "{0}, selected";
            return string.Format(CultureInfo.CurrentCulture, format, name);
        }
    }

    // The source button opens/closes popup content, so automation should expose Expand/Collapse semantics.
    public sealed class SourceSelectToggleButton : ToggleButton
    {
        // We cache the prior state because Checked/Unchecked handlers only expose the new state.
        private ExpandCollapseState _lastExpandCollapseState = ExpandCollapseState.Collapsed;

        public SourceSelectToggleButton()
        {
            Checked += SourceSelectToggleButton_CheckedChanged;
            Unchecked += SourceSelectToggleButton_CheckedChanged;
        }

        private void SourceSelectToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var newState = GetExpandCollapseState(IsChecked);
            if (newState != _lastExpandCollapseState && UIElementAutomationPeer.FromElement(this) is SourceSelectToggleButtonAutomationPeer peer)
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

    internal sealed class SourceSelectToggleButtonAutomationPeer : ToggleButtonAutomationPeer, IExpandCollapseProvider
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

        public override object? GetPattern(PatternInterface patternInterface)
        {
            // Expose ExpandCollapse and hide Toggle so assistive tech treats this as a popup opener.
            return patternInterface switch
            {
                PatternInterface.ExpandCollapse => this,
                PatternInterface.Toggle => null,
                _ => base.GetPattern(patternInterface),
            };
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
    }
}

#pragma warning restore CS1591