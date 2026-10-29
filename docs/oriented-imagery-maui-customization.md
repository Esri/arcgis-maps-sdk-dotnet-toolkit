# Customizing the MAUI oriented imagery view

The default `OrientedImageryView` uses platform-adaptive controls. You can replace
toolbar items with `ToolbarItemTemplateSelector`, reuse their built-in presentation,
or compose a control template from the default content. None of these customizations
requires changing the shared view model or the other toolbar items.

The **Oriented imagery customization** MAUI sample demonstrates both examples below.
Its options are initially off. The regular oriented imagery sample is unchanged.

## Reuse a built-in toolbar item

Use `OrientedImageryToolbarItemView` inside an item `DataTemplate` to retain the
platform's icon, control, enabled-state rules and interaction behavior. Set `Owner`
to the viewer. The template's binding context is the toolbar item, not the viewer's
view model.

For example, define an application-owned style:

```xml
<!-- xmlns:toolkit="clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui;assembly=Esri.ArcGISRuntime.Toolkit.Maui" -->
<Style x:Key="FootprintItemStyle"
       TargetType="toolkit:OrientedImageryToolbarItemView">
    <Setter Property="Text" Value="Footprint" />
    <Setter Property="TextColor"
            Value="{AppThemeBinding Light=#005E95, Dark=#6CB8EC}" />
    <Setter Property="FontSize" Value="15" />
    <Setter Property="Spacing" Value="10" />
    <Setter Property="Padding" Value="4" />
</Style>
```

Register a template after `InitializeComponent()`:

```csharp
var selector = new OrientedImageryViewToolbarSelector();
selector.SelectionPairs.Add(new OrientedImageryToolbarItemSelectorPair
{
    ItemType = typeof(ShowSelectedFootprintVM),
    Template = new DataTemplate(() => new OrientedImageryToolbarItemView
    {
        Owner = ImageryView,
        Style = (Style)Resources["FootprintItemStyle"],
    }),
});
ImageryView.ToolbarItemTemplateSelector = selector;
```

The current-footprint item now says **Footprint**, keeping its built-in icon and
toggle behavior. On Android/iOS it appears inside the existing Overlays sheet.
On Windows it retains the native toolbar control. Unmatched items and categories
keep their default presentation.

Styles can live in application resource dictionaries and use `BasedOn`. Use
application-localized resources instead of literal UI strings in production.
The sample uses toolkit-localized strings.

| Property | Meaning |
|---|---|
| `Owner` | Viewer supplying state and interactions. The item must belong to its current view model. |
| `Text` | Primary caption. Null retains the localized default. Does not rename individual mode choices or marker symbols. Mobile mode selection has separate option captions rather than a primary caption. |
| `AccessibilityText` | Primary accessible name and, on Windows, tooltip. When unset, a custom `Text` is used; otherwise the original localized name remains. Individual choices retain their own accessible names. |
| `TextColor` | Caption color. Null retains native theme/state colors. Windows high contrast takes precedence over custom caption colors. |
| `FontSize` | Caption font size before accessibility scaling. Null retains the platform default. Must be finite and greater than zero. |
| `Spacing` | Icon/caption gap: horizontal in mobile rows, vertical in Windows toolbar captions. Null retains the default. Must be finite and nonnegative. |
| Inherited `Padding`, `Margin`, sizing and `BackgroundColor` | Apply to the outer host, not the native control's internal state template or native sheet chrome. |

Clearing an override restores the default presentation. An arbitrary custom toolbar
item has no built-in renderer: supply an application-owned view instead. The host
does not call the selector recursively.

## Rearrange the default control template

`OrientedImageryView.DefaultControlTemplate` exposes the same template used by an
uncustomized viewer. Create fresh content for every template instance. Do not share
a materialized view tree, reparent native children into unrelated namescopes, or
modify the shared template object.

This example moves the existing toolbar below the viewport without recreating
pagination, status, native tools or image details:

```csharp
ImageryView.ControlTemplate = new ControlTemplate(() =>
{
    var content = (View)OrientedImageryView.DefaultControlTemplate.CreateContent();
    var layout = content.FindByName<Grid>("PART_LayoutRoot");
    var image = content.FindByName<Grid>("PART_ImageHost");
    var toolbar = content.FindByName<View>("PART_ToolbarScroll")
        ?? content.FindByName<Layout>("PART_CustomToolbar");

    layout.RowDefinitions[0].Height = GridLength.Star;
    layout.RowDefinitions[1].Height = GridLength.Auto;
    Grid.SetRow(image, 0);
    Grid.SetRow(toolbar, 1);
    return content;
});
```

Restore the default with:

```csharp
ImageryView.ControlTemplate = OrientedImageryView.DefaultControlTemplate;
```

### Template parts

For composition, retain the default namescope and the children of these sections:

| Part | Type and purpose |
|---|---|
| `PART_LayoutRoot` | `Grid`: outer rows containing toolbar, image area, pagination and notice. Also receives Windows system background updates. |
| `PART_ImageHost` | `Grid`: bounded viewport, status, arrows and details presentation. |
| `PART_CustomToolbar` | `Layout`: populated by the viewer using categories and the template selector. |
| `PART_ToolbarScroll` | Non-Windows `ScrollView` around the toolbar; move this instead of extracting its contents. |

For an entirely application-owned template, the main functional parts are:

| Part | Purpose |
|---|---|
| `PART_Viewport` (`OrientedImageViewport`) | Image rendering and image-tap interactions. |
| `PART_CustomToolbar` (`Layout`) | Default/custom category presentation. Omit only if the application supplies its own controls. |
| `PART_Status` (`Label`), `PART_StatusHost` (`View`) | Current status/error display. `ErrorMessage` and `HasSearchedForImages` can also be bound by custom UI. |
| `PART_Notice` (`Label`) | Sequential-navigation error text. |
| `PART_Busy` (`ActivityIndicator`, optional) | Opt-in loading spinner; not present in the default template. |
| `PART_Popup` (`PopupViewer`) and popup open/close/host parts | Image-details presentation and dismissal. |

The default template also includes platform-specific pagination, selection guidance
and native presentation parts. Some use internal helpers. **Use composition when
you want to preserve those features**, rather than reproducing their named parts
with different types. Omitting parts removes their corresponding UI; it does not
cause the control to generate a replacement automatically.

Native Android/iOS sheet chrome is not part of the main MAUI template and has no
separate public sheet template. MAUI and WPF templates are not interchangeable.
