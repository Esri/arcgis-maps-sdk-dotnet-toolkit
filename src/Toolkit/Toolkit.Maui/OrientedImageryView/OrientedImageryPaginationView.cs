#if ANDROID || __IOS__
namespace Esri.ArcGISRuntime.Toolkit.Maui;

// Stays alive while its tappable children rebuild, preserving the single navigation focus stop.
internal sealed class OrientedImageryPaginationView : ContentView
{
    #region Navigation state

    internal Action<bool>? Navigate { get; set; }

    internal bool CanGoPrevious { get; private set; }

    internal bool CanGoNext { get; private set; }

    internal string Value { get; private set; } = string.Empty;

    #endregion

    #region Guarded navigation and native updates

    internal bool Move(bool next)
    {
        if (!(next ? CanGoNext : CanGoPrevious) || Navigate == null)
            return false;
        Navigate(next);
        return true;
    }

    internal void Update(string value, bool previous, bool next)
    {
        if (Value == value && CanGoPrevious == previous && CanGoNext == next)
            return;
        Value = value;
        CanGoPrevious = previous;
        CanGoNext = next;
        (Handler as OrientedImageryPaginationViewHandler)?.UpdateAccessibility();
    }

    #endregion
}
#endif
