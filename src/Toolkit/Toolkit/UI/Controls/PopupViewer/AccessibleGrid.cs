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
#if WPF || WINUI

#if WPF
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
#elif WINUI
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    internal sealed partial class AccessibleGrid : Grid
    {
        /// <summary>
        /// Gets or sets whether this element is exposed to UI Automation as a real control/content element,
        /// overriding WPF's default suppression of elements whose TemplatedParent is set. Defaults to
        /// <c>true</c>; set to <c>false</c> (or bind it) to fall back to the normal suppressed behavior for a
        /// specific instance without needing a different element type.
        /// </summary>
        public bool IsAccessible
        {
            get => (bool)GetValue(IsAccessibleProperty);
            set => SetValue(IsAccessibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsAccessible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAccessibleProperty =
            DependencyProperty.Register(nameof(IsAccessible), typeof(bool), typeof(AccessibleGrid), new PropertyMetadata(true));

        public AutomationHeadingLevel AutomationHeadingLevel
        {
            get => (AutomationHeadingLevel)GetValue(AutomationHeadingLevelProperty);
            set => SetValue(AutomationHeadingLevelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutomationHeadingLevel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutomationHeadingLevelProperty =
            DependencyProperty.Register(nameof(AutomationHeadingLevel), typeof(AutomationHeadingLevel), typeof(AccessibleGrid), new PropertyMetadata(AutomationHeadingLevel.None));

        protected override AutomationPeer OnCreateAutomationPeer() => new AccessibleGridAutomationPeer(this);
    }

    internal sealed partial class AccessibleGridAutomationPeer : FrameworkElementAutomationPeer, IGridProvider
    {
        private readonly AccessibleGrid _owner;

        public AccessibleGridAutomationPeer(AccessibleGrid owner)
            : base(owner)
        {
            _owner = owner;
        }

        private bool IsAccessible => _owner?.IsAccessible ?? true;

        /// <inheritdoc />
        protected override bool IsControlElementCore() => IsAccessible;

        /// <inheritdoc />
        protected override bool IsContentElementCore() => IsAccessible;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Table;
        }

        protected override string GetNameCore() => string.Empty;

        public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;

        public int ColumnCount => 2;

        public int RowCount => _owner.RowDefinitions.Count;

        public IRawElementProviderSimple GetItem(int row, int column)
        {
            foreach (UIElement child in _owner.Children)
            {
                if (child is IFieldsTableCell cell && cell.Row == row && cell.Column == column)
                {
#if WPF
                    var peer = UIElementAutomationPeer.CreatePeerForElement(child);
#elif WINUI
                    var peer = FrameworkElementAutomationPeer.FromElement(child);
#endif
                    return peer is null ? null! : ProviderFromPeer(peer);
                }
            }
            return null!;
        }

#if WPF
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Grid)
                return this;
            return base.GetPattern(patternInterface);
        }
#elif WINUI
        protected override object GetPatternCore(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Grid)
                return this;
            return base.GetPatternCore(patternInterface);
        }
#endif
    }
}
#endif
