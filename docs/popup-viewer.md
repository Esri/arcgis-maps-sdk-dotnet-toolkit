# PopupViewer

Displays information from simple [popups](https://pro.arcgis.com/en/pro-app/latest/help/mapping/navigation/configure-pop-ups.htm). 

<img src="https://user-images.githubusercontent.com/3878047/228625594-d0009216-0cb3-4836-9a62-e197371779ac.png" width="120" />

## Features

- Supports limited text display from HTML-based popups.
- Supports charts, media, attachments, edit summary.
- Supports basic display of fields and values in non-HTML-based popups.

### EditSummary
`Popup.EditSummary` is a localized summary of when the popup was last edited or created by an editor or author respectively. The implementation formats the edit/creation date conditionally depending on whether the date is less than a week old or a week or more old.

Dates less than a week ago are formatted as relative dates such as "seconds ago", "a minute ago", "2 minutes ago", "an hour ago", "2 hours ago", "Wednesday at 12:34 PM".
Dates a week or more ago are formatted using the .NET "general date short format" ("g"), which corresponds to the pattern "M/d/yyyy h:mm tt" (for example, "6/15/2009 1:45 PM"). See [general date short format](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#the-general-date-short-time-g-format-specifier) for details.

If editor tracking is disabled for the popup, then the edit summary string will be `null` and will not be displayed.

<img src="https://github.com/user-attachments/assets/a48d2d20-6c9c-4bf9-9090-6641225bfc96" width="100"/>

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
