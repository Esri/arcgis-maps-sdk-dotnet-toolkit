# PopupViewer

Displays information from simple [popups](https://pro.arcgis.com/en/pro-app/latest/help/mapping/navigation/configure-pop-ups.htm). 

<img src="https://user-images.githubusercontent.com/1378165/73389991-1e549080-428a-11ea-81f3-b2f9c29f61ad.png" width="120" />

## Features

- Supports limited text display from HTML-based popups.
- Supports basic display of fields and values in non-HTML-based popups.

> **Important**: PopupViewer does not support charts or multimedia.

## Usage

PopupViewer displays popup information using an underlying [`PopupManager`](https://developers.arcgis.com/net/api-reference/api/netwin/Esri.ArcGISRuntime/Esri.ArcGISRuntime.Mapping.Popups.PopupManager.html).

The following code shows how to get a `Popup` for an identify result:

```cs
private Popup? GetPopup(IdentifyLayerResult result)
{
    if (result?.Popups?.FirstOrDefault() is Popup popup)
    {
        return popup;
    }

    if (result?.GeoElements?.FirstOrDefault() is GeoElement geoElement)
    {
        if (result.LayerContent is IPopupSource)
        {
            var popupDefinition = ((IPopupSource)result.LayerContent).PopupDefinition;
            if (popupDefinition != null)
            {
                return new Popup(geoElement, popupDefinition);
            }
        }

        return Popup.FromGeoElement(geoElement);
    }

    return null;
}
```

The following code shows how to get a `PopupManager` from a `Popup`:

```cs
var manager = new PopupManager(popup);
```

To display a `PopupViewer` in the UI:

```xml
<esri:PopupViewer x:Name="popupViewer" />
```

To present a `PopupManager` in a `PopupViewer`:

```cs
popupViewer.PopupManager = popupManager;
```