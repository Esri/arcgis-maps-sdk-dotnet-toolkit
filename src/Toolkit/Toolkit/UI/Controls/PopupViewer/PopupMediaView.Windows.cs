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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.Primitives;
using Esri.ArcGISRuntime.UI;
using System.IO;
#if WPF
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xaml;
#elif WINUI
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;
#elif WINDOWS_XAML
using Windows.Foundation;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="PopupMedia"/>.
    /// </summary>
    public partial class PopupMediaView : ContentControl
    {
        // A plain ContentControl has no automation peer by default. Without this override, the enclosing
        // media list's ItemsControlAutomationPeer can't find a real peer for this item and falls back to a
        // synthetic ItemAutomationPeer wrapping the raw PopupMedia data object - so Narrator reads the data
        // object's ToString() ("Esri.ArcGISRuntime.Mapping.Popups.PopupMedia") instead of this view's content.
#if WPF
        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new PopupMediaViewPeer(this);
#elif WINUI
        /// <inheritdoc />
        protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
#endif

#if WPF
        /// <inheritdoc />
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // The accessible name comes from the (asynchronously generated) content, so let UIA clients know it changed.
            if (UIElementAutomationPeer.FromElement(this) is AutomationPeer peer)
            {
                var oldName = oldContent is DependencyObject o ? System.Windows.Automation.AutomationProperties.GetName(o) : string.Empty;
                peer.RaisePropertyChangedEvent(System.Windows.Automation.AutomationElementIdentifiers.NameProperty, oldName, peer.GetName());
            }
        }

        /// <inheritdoc />
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            _lastChartSize = 0;
            base.OnDpiChanged(oldDpi, newDpi);
        }
#endif
        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            if (PopupMedia != null && PopupMedia.Type != PopupMediaType.Image)
            {
                UpdateChart(constraint);
            }
            return base.MeasureOverride(constraint);
        }

    }
}
#endif

#if WPF
/// <summary>
/// Automation peer for the <see cref="PopupMediaView"/> control.
/// </summary>
/// <remarks>
/// Exposes the media view as a single image element named after the media's alternative text, and hides the
/// generated image inside it so the alternative text is only announced once. Exposing this element (rather than
/// the inner image) matters because <see cref="MediaPopupElementView"/> sets PositionInSet/SizeOfSet on it,
/// so Narrator can announce "item X of Y" for the current media item.
/// </remarks>
public class PopupMediaViewPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Creates a new instance of the <see cref="PopupMediaViewPeer"/> class.
    /// </summary>
    /// <param name="owner"></param>
    public PopupMediaViewPeer(PopupMediaView owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(PopupMediaView);

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

    /// <summary>
    /// Returns the alternative text of the displayed media, which is set as the accessible name of the generated image content.
    /// </summary>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (string.IsNullOrEmpty(name) && Owner is PopupMediaView view && view.Content is DependencyObject content)
        {
            name = System.Windows.Automation.AutomationProperties.GetName(content);
        }
        return name ?? string.Empty;
    }

    /// <summary>
    /// Hides the generated image content, whose alternative text is already exposed as this element's name.
    /// </summary>
    protected override List<AutomationPeer>? GetChildrenCore() => null;
}
#endif