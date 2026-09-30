using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Color = System.Drawing.Color;

namespace Toolkit.Tests;

/// <summary>
/// Contracts of <see cref="OrientedImageDisplay"/> and its inner displays that hold without a running app:
/// template re-hosting, marker subscriptions that must not retain a discarded display, marker offsets and hit-testing,
/// accessibility, and the visible-area clipping math.
/// </summary>
[TestClass]
public sealed class OrientedImageDisplayTests
{
    [TestMethod]
    public void ReapplyingTemplateRehostsActiveDisplay()
    {
        RunSta(() =>
        {
            var control = new OrientedImageDisplay { Template = CreateHostTemplate() };
            Assert.IsTrue(control.ApplyTemplate());
            ContentPresenter firstHost = GetHost(control);
            object? display = firstHost.Content;
            Assert.IsNotNull(display, "the initial template's host presents the active display");

            control.Template = CreateHostTemplate();
            Assert.IsTrue(control.ApplyTemplate());
            ContentPresenter secondHost = GetHost(control);

            Assert.AreNotSame(firstHost, secondHost, "sanity: re-applying the template creates a new host");
            Assert.IsNull(firstHost.Content, "the discarded template's host must release the display");
            Assert.AreSame(display, secondHost.Content, "the re-applied template's host must adopt the same active display");
        });
    }

    [TestMethod]
    public void MarkerSubscriptionDoesNotRetainDiscardedDisplay()
    {
        RunSta(() =>
        {
            var markers = new ObservableCollection<OrientedImageMarker>
            {
                new(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0))),
            };

            WeakReference weakDisplay = CreateDiscardedDisplay(markers);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(
                weakDisplay.IsAlive,
                "an app-owned marker's PropertyChanged subscription must not keep a discarded display alive");
        });
    }

    // Not inlined, so no caller register/local can keep the display reachable across the collection above.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDiscardedDisplay(ObservableCollection<OrientedImageMarker> markers)
    {
        var display = new OrientedImagePanoramicDisplay();
        display.SetMarkers(markers);
        return new WeakReference(display);
    }

    [TestMethod]
    public void PanoramicMarkerOffsetTurnsWithSymbolAngle()
    {
        // The map rotates a marker clockwise around its anchor, offset included; screen y points down.
        AssertOffset(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10), 0, 0);
        AssertOffset(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10) { OffsetX = 15, OffsetY = 20 }, 15, -20);
        AssertOffset(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10) { OffsetX = 10, Angle = 90 }, 0, 10);
        AssertOffset(new PictureMarkerSymbol(new Uri("https://example.com/pin.png")) { OffsetY = 16, Angle = 90 }, 16, 0);
        AssertOffset(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 2), 0, 0);

        static void AssertOffset(Symbol symbol, double x, double y)
        {
            (double X, double Y) offset = OrientedImagePanoramicDisplay.GetMarkerOffset(symbol);
            Assert.AreEqual(x, offset.X, 1e-9, $"{symbol.GetType().Name} X");
            Assert.AreEqual(y, offset.Y, 1e-9, $"{symbol.GetType().Name} Y");
        }
    }

    [TestMethod]
    public void PanoramicHitTestFollowsTheDrawnMarker()
    {
        // Looking at the image center: (0.75, 0.5) projects to the middle of a 400 x 300 view.
        var camera = new PanoramaCameraState(yaw: 0f, pitch: 0f, fieldOfView: MathF.PI / 2f);
        Assert.IsTrue(camera.TryNormalizedUvToScreen(0.75f, 0.5f, 400, 300, out double cx, out double cy));
        var onAnchor = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0)));
        var nearAnchor = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0)));
        var drawnAway = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0)));

        // Drawn 15 DIPs right of and 20 DIPs above its anchor.
        var offset = new[] { new OrientedImagePanoramicDisplay.ResolvedMarker(drawnAway, 0.75f, 0.5f, 15, -20) };
        Assert.AreSame(drawnAway, Hit(offset, cx + 15, cy - 20, dip: 1), "a tap on the drawn marker hits it");
        Assert.IsNull(Hit(offset, cx, cy, dip: 1), "a tap on the bare anchor, 25 DIPs from the drawing, misses");
        Assert.AreSame(drawnAway, Hit(offset, cx + 30, cy - 40, dip: 2), "the offset scales with the view's pixels per DIP");

        var close = new[]
        {
            new OrientedImagePanoramicDisplay.ResolvedMarker(onAnchor, 0.75f, 0.5f, 0, 0),
            new OrientedImagePanoramicDisplay.ResolvedMarker(nearAnchor, 0.75f, 0.5f, 6, 0),
        };
        Assert.AreSame(onAnchor, Hit(close, cx + 2, cy, dip: 1), "the nearest drawn marker wins");
        Assert.AreSame(nearAnchor, Hit(close, cx + 4, cy, dip: 1), "the nearest drawn marker wins");

        OrientedImageMarker? Hit(OrientedImagePanoramicDisplay.ResolvedMarker[] markers, double x, double y, double dip) =>
            OrientedImagePanoramicDisplay.HitTestMarker(markers, camera, 400, 300, x, y, dip);
    }

    [TestMethod]
    public void PanoramicSurfaceHasAccessibleName()
    {
        RunSta(() =>
        {
            // The surface is the keyboard-focusable element of the panoramic display; it must carry an accessible
            // name (the raster display labels its inner MapView the same way).
            var display = new OrientedImagePanoramicDisplay();
            var surface = (DependencyObject)display.Content!;
            string name = System.Windows.Automation.AutomationProperties.GetName(surface);
            Assert.IsFalse(string.IsNullOrEmpty(name), "the panoramic surface must have an automation name");
        });
    }

    [TestMethod]
    public void VisibleAreaIsClippedToImageNotClamped()
    {
        // A 45deg-rotated view (diamond) that fully CONTAINS the 100x100 image: the correct visible-area footprint
        // is the whole image (area 10000). Clamping each vertex independently would instead collapse the ring to
        // the diamond of the edge midpoints - half the actual footprint (area 5000).
        var builder = new Esri.ArcGISRuntime.Geometry.PolygonBuilder((Esri.ArcGISRuntime.Geometry.SpatialReference?)null);
        builder.AddPoint(50, 150);
        builder.AddPoint(150, 50);
        builder.AddPoint(50, -50);
        builder.AddPoint(-50, 50);
        var visibleArea = builder.ToGeometry();
        var extent = new Esri.ArcGISRuntime.Geometry.Envelope(0, 0, 100, 100);

        List<System.Drawing.PointF> ring = OrientedImageRasterDisplay.ComputeVisibleAreaPixels(visibleArea, extent, 1, 1);

        Assert.IsTrue(ring.Count >= 4, $"expected a full ring, got {ring.Count} vertices");
        Assert.AreEqual(10000d, RingArea(ring), 1e-3, "the clipped footprint must cover the whole image");
    }

    private static double RingArea(IReadOnlyList<System.Drawing.PointF> ring)
    {
        double area = 0;
        for (int i = 0; i < ring.Count; i++)
        {
            System.Drawing.PointF a = ring[i];
            System.Drawing.PointF b = ring[(i + 1) % ring.Count];
            area += ((double)a.X * b.Y) - ((double)b.X * a.Y);
        }

        return Math.Abs(area) / 2;
    }

    // Mirrors the shape of the control's default template: a single named host presenter.
    private static ControlTemplate CreateHostTemplate() => new(typeof(OrientedImageDisplay))
    {
        VisualTree = new FrameworkElementFactory(typeof(ContentPresenter), "PART_DisplayHost"),
    };

    private static ContentPresenter GetHost(OrientedImageDisplay control)
    {
        var host = control.Template.FindName("PART_DisplayHost", control) as ContentPresenter;
        Assert.IsNotNull(host, "the applied template must contain PART_DisplayHost");
        return host;
    }

    // WPF elements require an STA thread; MSTest test threads are MTA.
    private static void RunSta(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                test();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
