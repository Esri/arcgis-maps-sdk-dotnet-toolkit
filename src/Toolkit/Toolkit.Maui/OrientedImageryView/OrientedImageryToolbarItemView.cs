namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Presents a built-in oriented imagery toolbar item using the current platform's controls.
/// </summary>
/// <remarks>
/// Set <see cref="Owner"/> to the viewer and use a built-in <see cref="OrientedImageryToolbarItemBase"/>
/// as the binding context. Unset appearance properties retain the platform defaults.
/// </remarks>
public sealed class OrientedImageryToolbarItemView : ContentView
{
    private OrientedImageryView? _subscribedOwner;
    private OrientedImageryToolbarItemPresentation? _presentation;
    private bool _unloaded;

    /// <summary>Identifies the <see cref="Owner"/> bindable property.</summary>
    public static readonly BindableProperty OwnerProperty = BindableProperty.Create(
        nameof(Owner), typeof(OrientedImageryView), typeof(OrientedImageryToolbarItemView), null,
        propertyChanged: AppearanceChanged);

    /// <summary>Identifies the <see cref="Text"/> bindable property.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(OrientedImageryToolbarItemView), null,
        propertyChanged: AppearanceChanged);

    /// <summary>Identifies the <see cref="AccessibilityText"/> bindable property.</summary>
    public static readonly BindableProperty AccessibilityTextProperty = BindableProperty.Create(
        nameof(AccessibilityText), typeof(string), typeof(OrientedImageryToolbarItemView), null,
        propertyChanged: AppearanceChanged);

    /// <summary>Identifies the <see cref="TextColor"/> bindable property.</summary>
    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(OrientedImageryToolbarItemView), null,
        propertyChanged: AppearanceChanged);

    /// <summary>Identifies the <see cref="FontSize"/> bindable property.</summary>
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double?), typeof(OrientedImageryToolbarItemView), null,
        validateValue: (_, value) => value == null || value is double size && double.IsFinite(size) && size > 0,
        propertyChanged: AppearanceChanged);

    /// <summary>Identifies the <see cref="Spacing"/> bindable property.</summary>
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double?), typeof(OrientedImageryToolbarItemView), null,
        validateValue: (_, value) => value == null || value is double spacing && double.IsFinite(spacing) && spacing >= 0,
        propertyChanged: AppearanceChanged);

    /// <summary>Gets or sets the viewer whose state and actions the item uses.</summary>
    public OrientedImageryView? Owner
    {
        get => (OrientedImageryView?)GetValue(OwnerProperty);
        set => SetValue(OwnerProperty, value);
    }

    /// <summary>
    /// Gets or sets the primary caption override. Null retains the localized caption.
    /// Multiple-choice option labels are not replaced.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the accessible description. Null uses <see cref="Text"/> when supplied,
    /// otherwise the localized description. Multiple-choice options retain their own names.
    /// </summary>
    public string? AccessibilityText
    {
        get => (string?)GetValue(AccessibilityTextProperty);
        set => SetValue(AccessibilityTextProperty, value);
    }

    /// <summary>Gets or sets the caption color. Null retains native theme and state colors.</summary>
    public Color? TextColor
    {
        get => (Color?)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    /// <summary>Gets or sets the caption font size before accessibility scaling. Null retains the default.</summary>
    public double? FontSize
    {
        get => (double?)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>Gets or sets the icon/caption gap. Null retains the platform default.</summary>
    public double? Spacing
    {
        get => (double?)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>Initializes a new instance of the <see cref="OrientedImageryToolbarItemView"/> class.</summary>
    public OrientedImageryToolbarItemView()
    {
        Loaded += (_, _) =>
        {
            _unloaded = false;
            Rebuild();
        };
        Unloaded += (_, _) =>
        {
            _unloaded = true;
            Release();
        };
    }

    private static void AppearanceChanged(BindableObject sender, object oldValue, object newValue) =>
        ((OrientedImageryToolbarItemView)sender).Rebuild();

    /// <inheritdoc/>
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Rebuild();
    }

    /// <inheritdoc/>
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        Release();
        base.OnHandlerChanging(args);
    }

    /// <inheritdoc/>
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        Rebuild();
    }

    private void Rebuild()
    {
        Release();
        if (_unloaded || Handler == null || Owner == null || BindingContext == null)
            return;
        if (BindingContext is not OrientedImageryToolbarItemBase item)
            throw new InvalidOperationException("An oriented imagery toolbar item view requires a toolbar item as its binding context.");

        _presentation = Owner.CreateToolbarItemPresentation(this, item);
        Content = _presentation.Content;
        _subscribedOwner = Owner;
        _subscribedOwner.ToolbarPresentationUpdated += OwnerUpdated;
        _presentation.Update();
    }

    private void OwnerUpdated(object? sender, EventArgs e)
    {
        if (_presentation?.Model != Owner?.ViewModel || _presentation?.Layer != Owner?.ViewModel.OrientedImageryLayer)
            Rebuild();
        else
            _presentation?.Update();
    }

    private void Release()
    {
        if (_subscribedOwner != null)
            _subscribedOwner.ToolbarPresentationUpdated -= OwnerUpdated;
        _subscribedOwner = null;
        _presentation?.Dispose();
        _presentation = null;
        Content = null;
    }
}

internal sealed class OrientedImageryToolbarItemPresentation(
    OrientedImageryToolbarItemView host, OrientedImageryViewModel model) : IDisposable
{
    internal OrientedImageryViewModel Model { get; } = model;
    internal Esri.ArcGISRuntime.Mapping.OrientedImageryLayer? Layer { get; } = model.OrientedImageryLayer;
    internal View? Content { get; set; }
    internal List<Action> Updates { get; } = new();
    internal List<Action> Cleanup { get; } = new();
    internal bool Updating { get; private set; }
    internal bool IsDisposed { get; private set; }
    internal bool CanInteract => !IsDisposed && !Updating && host.IsEnabled &&
        host.Owner?.ViewModel == Model && Model.OrientedImageryLayer == Layer && host.Owner.CanUseToolbarItem;
    internal OrientedImageryToolbarItemView Host => host;

    internal void Update()
    {
        if (IsDisposed || Updating) return;
        Updating = true;
        try
        {
            foreach (var update in Updates.ToArray())
                update();
        }
        finally
        {
            Updating = false;
        }
    }

    public void Dispose()
    {
        IsDisposed = true;
        foreach (var cleanup in Cleanup)
            cleanup();
        Cleanup.Clear();
        Updates.Clear();
        Content = null;
    }
}
