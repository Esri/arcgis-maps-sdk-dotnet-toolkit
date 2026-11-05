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
#if WPF
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    internal sealed class AccessibleRichTextBox : RichTextBox
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
            DependencyProperty.Register(nameof(IsAccessible), typeof(bool), typeof(AccessibleRichTextBox), new PropertyMetadata(true));

        public AutomationHeadingLevel AutomationHeadingLevel
        {
            get => (AutomationHeadingLevel)GetValue(AutomationHeadingLevelProperty);
            set => SetValue(AutomationHeadingLevelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutomationHeadingLevel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutomationHeadingLevelProperty =
            DependencyProperty.Register(nameof(AutomationHeadingLevel), typeof(AutomationHeadingLevel), typeof(AccessibleRichTextBox), new PropertyMetadata(AutomationHeadingLevel.None));

        protected override AutomationPeer OnCreateAutomationPeer() => new AccessibleRichTextBoxAutomationPeer(this);
    }

    internal sealed class AccessibleRichTextBoxAutomationPeer : RichTextBoxAutomationPeer
    {
        public AccessibleRichTextBoxAutomationPeer(AccessibleRichTextBox owner)
            : base(owner)
        {
        }

        private bool IsAccessible => (Owner as AccessibleRichTextBox)?.IsAccessible ?? true;

        /// <inheritdoc />
        protected override bool IsControlElementCore() => IsAccessible;

        /// <inheritdoc />
        protected override bool IsContentElementCore() => IsAccessible;

        protected override AutomationHeadingLevel GetHeadingLevelCore() => (Owner as AccessibleRichTextBox)?.AutomationHeadingLevel ?? base.GetHeadingLevelCore();
    }
}
#endif
