#if WPF

using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageViewportName, Type = typeof(OrientedImageViewport))]
[TemplatePart(Name = ToolbarContainerName, Type = typeof(ItemsControl))]
[TemplatePart(Name = PaginatorName, Type = typeof(Paginator))]
public partial class OrientedImageryView : Control
{
}

#endif