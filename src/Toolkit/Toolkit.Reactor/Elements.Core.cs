using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public abstract record GeoViewElement(Action<GeoViewInputEventArgs>? OnTapped = null) : Element
{
    internal Action<GeoView>[] Setters { get; init; } = [];
    public GraphicsOverlayCollection? GraphicsOverlays { get; set; }
}

public record MapViewElement(Map? Map, Action<GeoViewInputEventArgs>? OnTapped = null) : GeoViewElement(OnTapped)
{
}

public record SceneViewElement(Scene? Scene, Action<GeoViewInputEventArgs>? OnTapped = null) : GeoViewElement(OnTapped)
{
}

public record LocalSceneViewElement(Scene? Scene, Action<GeoViewInputEventArgs>? OnTapped = null) : GeoViewElement(OnTapped)
{
}


