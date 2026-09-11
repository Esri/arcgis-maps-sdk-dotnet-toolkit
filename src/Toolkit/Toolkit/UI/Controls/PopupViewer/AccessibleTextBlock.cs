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
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    // A drop-in replacement for TextBlock used for the Title/Description/Caption headers written directly
    // inside a PopupViewer sub-element's ControlTemplate (e.g. FieldsPopupElementView, MediaPopupElementView).
    // WPF sets TemplatedParent on any element authored directly in a ControlTemplate, and TextBlockAutomationPeer
    // defaults IsControlElement/IsContentElement to false whenever TemplatedParent is set - so a plain
    // TextBlock there is silently invisible to Narrator's normal navigation regardless of AutomationProperties.Name.
    // This subclass forces both flags back to true (via IsAccessible) so the header is a real, reachable stop,
    // while keeping TextBlockAutomationPeer's normal ControlType/text behavior for everything else.
    internal sealed class AccessibleTextBlock : TextBlock
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
            DependencyProperty.Register(nameof(IsAccessible), typeof(bool), typeof(AccessibleTextBlock), new PropertyMetadata(true));

        protected override AutomationPeer OnCreateAutomationPeer() => new AccessibleTextBlockAutomationPeer(this);
    }

    internal sealed class AccessibleTextBlockAutomationPeer : TextBlockAutomationPeer
    {
        public AccessibleTextBlockAutomationPeer(TextBlock owner)
            : base(owner)
        {
        }

        private bool IsAccessible => (Owner as AccessibleTextBlock)?.IsAccessible ?? true;

        /// <inheritdoc />
        protected override bool IsControlElementCore() => IsAccessible;

        /// <inheritdoc />
        protected override bool IsContentElementCore() => IsAccessible;
    }
}
#endif
