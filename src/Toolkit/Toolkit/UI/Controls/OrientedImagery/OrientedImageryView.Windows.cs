#if WPF // Limiting this to WPF for now to keep things simple

using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageDisplayName, Type = typeof(OrientedImageDisplay))]
[TemplatePart(Name = ToolbarContainerName, Type = typeof(ItemsControl))]
public partial class OrientedImageryView : Control
{
}

#endif