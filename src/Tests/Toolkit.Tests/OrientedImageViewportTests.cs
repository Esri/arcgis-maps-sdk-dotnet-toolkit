using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;

namespace Toolkit.Tests;

/// <summary>
/// Contracts of <see cref="OrientedImageViewport"/> and its inner viewports that hold without a running app.
/// </summary>
[TestClass]
public sealed class OrientedImageViewportTests
{
    [STATestMethod]
    public void ReapplyingTemplateRehostsActiveViewport()
    {
        // Recreating the inner viewport would lose its image and navigation state.
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        Assert.IsTrue(control.ApplyTemplate());
        Border firstHost = GetHost(control);
        Assert.IsNull(firstHost.Child, "an empty viewport hosts no inner viewport");

        control.SetActiveViewport(new OrientedImageRasterViewport());
        UIElement? view = firstHost.Child;
        Assert.IsNotNull(view, "the host presents the active inner viewport's view");

        control.Template = CreateHostTemplate();
        Assert.IsTrue(control.ApplyTemplate());
        Border secondHost = GetHost(control);

        Assert.AreNotSame(firstHost, secondHost, "sanity: re-applying the template creates a new host");
        Assert.IsNull(firstHost.Child, "the discarded template's host must release the view");
        Assert.AreSame(view, secondHost.Child, "the re-applied template's host must adopt the same view");
    }

    [STATestMethod]
    public void EmptyViewportPaintsTheBackgroundColor()
    {
        // With no inner viewport, the host is the only thing that can show the configured color.
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        Assert.IsTrue(control.ApplyTemplate());
        Border host = GetHost(control);
        Assert.IsNull(host.Background, "the default leaves an empty viewport transparent");

        control.DisplayBackgroundColor = Color.SteelBlue;
        var brush = host.Background as SolidColorBrush;
        Assert.IsNotNull(brush, "a set color paints the host");
        Assert.AreEqual(System.Windows.Media.Color.FromArgb(255, 70, 130, 180), brush.Color);

        control.DisplayBackgroundColor = Color.Empty;
        Assert.IsNull(host.Background, "clearing the color clears the host");

        control.DisplayBackgroundColor = Color.SteelBlue;
        control.Template = CreateHostTemplate();
        Assert.IsTrue(control.ApplyTemplate());
        Assert.IsNotNull(GetHost(control).Background as SolidColorBrush, "a re-applied template is painted too");
    }

    [STATestMethod]
    public void InnerViewportsHideTheirViewWithoutAnImage()
    {
        // A shown view would paint its own backdrop over the host's color.
        Assert.AreEqual(Visibility.Collapsed, new OrientedImageRasterViewport().View.Visibility, "planar");
        Assert.AreEqual(Visibility.Collapsed, new OrientedImagePanoramicViewport().View.Visibility, "panoramic");
    }

    [STATestMethod]
    public void MarkerSubscriptionDoesNotRetainDiscardedViewport()
    {
        var markers = new ObservableCollection<OrientedImageMarker>
        {
            new(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0)), new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10)),
        };

        WeakReference weakViewport = CreateDiscardedViewport(markers);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(
            weakViewport.IsAlive,
            "an app-owned marker's PropertyChanged subscription must not keep a discarded panoramic viewport alive");
    }

    // Not inlined, so no local or register in the caller keeps the viewport reachable across the collection.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDiscardedViewport(ObservableCollection<OrientedImageMarker> markers)
    {
        var viewport = new OrientedImagePanoramicViewport();
        viewport.SetMarkers(markers);

        // The queued marker pass holds the viewport until the next UI turn.
        RunPendingDispatcherWork();
        return new WeakReference(viewport);
    }

    [STATestMethod]
    public void MarkerChangesStartOnlyTheNeededPasses()
    {
        var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10);
        var marker = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0)), symbol);
        var viewport = new OrientedImagePanoramicViewport();
        viewport.SetMarkers(new[] { marker });
        int passes = viewport.MarkerGeneration;

        marker.Tag = "changed";
        Assert.AreEqual(passes, viewport.MarkerGeneration, "Tag is not drawn");

        symbol.Color = Color.Blue;
        Assert.AreEqual(++passes, viewport.MarkerGeneration, "a symbol change");

        marker.IsVisible = false;
        Assert.AreEqual(++passes, viewport.MarkerGeneration, "a visibility change");

        marker.Position = OrientedImageMarkerPosition.FromLocation(new MapPoint(1, 1));
        Assert.AreEqual(++passes, viewport.MarkerGeneration, "a position change");

        var replacement = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, Color.Red, 10);
        marker.Symbol = replacement;
        Assert.AreEqual(++passes, viewport.MarkerGeneration, "a symbol replacement");
        symbol.Color = Color.Green;
        Assert.AreEqual(passes, viewport.MarkerGeneration, "the replaced symbol is no longer watched");
        replacement.Color = Color.Green;
        Assert.AreEqual(++passes, viewport.MarkerGeneration, "the new symbol is watched");

        viewport.SetMarkers(null);
        passes = viewport.MarkerGeneration;
        replacement.Color = Color.Blue;
        Assert.AreEqual(passes, viewport.MarkerGeneration, "a removed marker's symbol is no longer watched");
    }

    [STATestMethod]
    public void BurstOfMarkerChangesTakesOnePass()
    {
        var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10);
        List<OrientedImageMarker> markers = Enumerable.Range(0, 50)
            .Select(i => new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(i, 0)), symbol))
            .ToList();
        var viewport = new OrientedImagePanoramicViewport();
        viewport.SetMarkers(markers);
        RunPendingDispatcherWork();
        int passes = viewport.MarkerPasses;

        symbol.Color = Color.Blue;
        RunPendingDispatcherWork();
        Assert.AreEqual(passes + 1, viewport.MarkerPasses, "a shared symbol change");

        foreach (OrientedImageMarker marker in markers)
            marker.IsVisible = false;
        RunPendingDispatcherWork();
        Assert.AreEqual(passes + 2, viewport.MarkerPasses, "a loop over the markers");
    }

    // Flushes work posted to this thread's dispatcher at priorities above Background.
    private static void RunPendingDispatcherWork() =>
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { }));

    [TestMethod]
    public void PanoramicMarkerOffsetTurnsWithSymbolAngle()
    {
        // The offset turns clockwise with the angle, and screen y points down.
        AssertOffset(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10), 0, 0);
        AssertOffset(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10) { OffsetX = 15, OffsetY = 20 }, 15, -20);
        AssertOffset(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10) { OffsetX = 10, Angle = 90 }, 0, 10);
        AssertOffset(new PictureMarkerSymbol(new Uri("https://example.com/pin.png")) { OffsetY = 16, Angle = 90 }, 16, 0);
        AssertOffset(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 2), 0, 0);

        static void AssertOffset(Symbol symbol, double x, double y)
        {
            (double X, double Y) offset = OrientedImagePanoramicViewport.GetMarkerOffset(symbol);
            Assert.AreEqual(x, offset.X, 1e-9, $"{symbol.GetType().Name} X");
            Assert.AreEqual(y, offset.Y, 1e-9, $"{symbol.GetType().Name} Y");
        }
    }

    [TestMethod]
    public void PanoramicHitTestFollowsTheDrawnMarker()
    {
        var camera = new PanoramaCameraState(yaw: 0f, pitch: 0f, fieldOfView: MathF.PI / 2f);
        Assert.IsTrue(camera.TryNormalizedUvToScreen(0.75f, 0.5f, 400, 300, out double cx, out double cy));

        var offset = Marker(size: 10, offsetX: 15, offsetY: -20);
        Assert.AreSame(offset.Marker, Hit([offset], cx + 15, cy - 20), "a tap on the drawn marker hits it");
        Assert.IsNull(Hit([offset], cx, cy), "a tap on the bare anchor, 18 DIPs from the drawn marker, misses");

        // The whole symbol counts, plus 12 DIPs around it.
        var large = Marker(size: 80, offsetX: 0, offsetY: 0);
        Assert.IsNotNull(Hit([large], cx + 30, cy), "inside the symbol");
        Assert.IsNotNull(Hit([large], cx + 51, cy), "11 DIPs outside its edge");
        Assert.IsNull(Hit([large], cx + 55, cy), "15 DIPs outside its edge");

        // As in the planar viewport, the topmost marker within tolerance wins over a direct hit beneath it.
        var upper = Marker(size: 20, offsetX: 55, offsetY: 0);
        Assert.AreSame(upper.Marker, Hit([large, upper], cx + 38, cy), "inside the lower marker, 7 DIPs from the upper one");
        Assert.AreSame(large.Marker, Hit([large, upper], cx - 20, cy), "out of the upper marker's reach");

        OrientedImageMarker? Hit(OrientedImagePanoramicViewport.ResolvedMarker[] markers, double x, double y) =>
            OrientedImagePanoramicViewport.HitTestMarker(markers, camera, 400, 300, x, y);
    }

    [TestMethod]
    public void LookAtPutsThePixelAtTheViewCenter()
    {
        var camera = new PanoramaCameraState(yaw: 0.3f, pitch: -0.2f, fieldOfView: 1.2f);
        foreach ((float u, float v) in new[] { (0.75f, 0.5f), (0f, 0.5f), (0.1f, 0.3f), (0.6f, 0.9f), (0.999f, 0.1f) })
        {
            PanoramaCameraState aimed = camera.LookAt(u, v);
            Assert.AreEqual(1.2f, aimed.FieldOfView, $"({u}, {v}) keeps the field of view");
            Assert.IsTrue(aimed.TryScreenToNormalizedUv(200, 150, 400, 300, out float centerU, out float centerV));
            Assert.AreEqual(0f, WrappedDistance(u, centerU), 1e-3f, $"({u}, {v}) column at the center");
            Assert.AreEqual(v, centerV, 1e-3f, $"({u}, {v}) row at the center");
        }

        PanoramaCameraState turned = camera.LookAt(0.1f);
        Assert.AreEqual(-0.2f, turned.Pitch, "a column alone keeps the pitch");
        Assert.IsTrue(turned.TryScreenToNormalizedUv(200, 150, 400, 300, out float turnedU, out _));
        Assert.AreEqual(0f, WrappedDistance(0.1f, turnedU), 1e-3f, "the column at the center");

        Assert.AreEqual(PanoramaCameraState.MinPitch, camera.LookAt(0.5f, 0f).Pitch, "the zenith clamps to the pitch limit");
        Assert.AreEqual(PanoramaCameraState.MaxPitch, camera.LookAt(0.5f, 1f).Pitch, "the nadir clamps to the pitch limit");

        static float WrappedDistance(float a, float b) => MathF.Abs(((b - a + 1.5f) % 1f) - 0.5f);
    }

    [STATestMethod]
    public void LookAtWithoutAnImageIsFalse()
    {
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        Assert.IsTrue(control.ApplyTemplate());
        Assert.ThrowsExactly<ArgumentNullException>(() => control.LookAtAsync(null!));
        Assert.IsFalse(control.LookAtAsync(new MapPoint(0, 0)).Result, "with a template but no footprint");
        Assert.IsFalse(new OrientedImageViewport().LookAtAsync(new MapPoint(0, 0)).Result, "without a template or a footprint");
    }

    [TestMethod]
    public void StillImagesSpanning360DegreesArePanoramic()
    {
        // The SDK's image transforms treat any image that spans 360 degrees as spherical.
        Assert.IsTrue(OrientedImageViewport.IsPanoramic(OrientedImageType.Image360, new Dictionary<string, object?>()));
        Assert.IsTrue(OrientedImageViewport.IsPanoramic(OrientedImageType.Unknown, new Dictionary<string, object?> { ["HorizontalFieldOfView"] = 360d }));
        Assert.IsFalse(OrientedImageViewport.IsPanoramic(OrientedImageType.Horizontal, new Dictionary<string, object?> { ["HorizontalFieldOfView"] = 100.4d }));
    }

    [TestMethod]
    public void PanoramicHeadingTreatsUnknownAsNorth()
    {
        Assert.AreEqual(0f, OrientedImagePanoramicViewport.ReadHeadingRadians(new Dictionary<string, object?> { ["CameraHeading"] = -999d }));
        Assert.AreEqual(0f, OrientedImagePanoramicViewport.ReadHeadingRadians(new Dictionary<string, object?>()));
        Assert.AreEqual(MathF.PI / 2f, OrientedImagePanoramicViewport.ReadHeadingRadians(new Dictionary<string, object?> { ["CameraHeading"] = 90d }), 1e-6f);
    }

    // A marker at the identity camera's view center, with a square swatch of the given size in DIPs.
    private static OrientedImagePanoramicViewport.ResolvedMarker Marker(double size, double offsetX, double offsetY) =>
        new(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0))), 0.75f, 0.5f, offsetX, offsetY, size / 2, size / 2);

    [STATestMethod]
    public void PanoramicSurfaceHasAccessibleName()
    {
        // The surface, not the viewport, takes keyboard focus.
        var viewport = new OrientedImagePanoramicViewport();
        string name = System.Windows.Automation.AutomationProperties.GetName(viewport.View);
        Assert.IsFalse(string.IsNullOrEmpty(name), "the panoramic surface must have an automation name");
    }

    [STATestMethod]
    public void AutomationPropertiesOnControlReachFocusableView()
    {
        // Focus lands on the inner view, not the control, so the app's name and automation id must reach the view.
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        System.Windows.Automation.AutomationProperties.SetName(control, "Rear camera");
        System.Windows.Automation.AutomationProperties.SetAutomationId(control, "RearCamera");
        Assert.IsTrue(control.ApplyTemplate());
        control.SetActiveViewport(new OrientedImageRasterViewport());
        DependencyObject view = GetFocusableView(control);
        Assert.AreEqual("Rear camera", System.Windows.Automation.AutomationProperties.GetName(view));
        Assert.AreEqual("RearCamera", System.Windows.Automation.AutomationProperties.GetAutomationId(view));

        System.Windows.Automation.AutomationProperties.SetName(control, "Front camera");
        System.Windows.Automation.AutomationProperties.SetAutomationId(control, "FrontCamera");
        Assert.AreEqual("Front camera", System.Windows.Automation.AutomationProperties.GetName(view));
        Assert.AreEqual("FrontCamera", System.Windows.Automation.AutomationProperties.GetAutomationId(view));

        control.ClearValue(System.Windows.Automation.AutomationProperties.NameProperty);
        string defaultName = System.Windows.Automation.AutomationProperties.GetName(view);
        Assert.IsFalse(string.IsNullOrEmpty(defaultName), "the view must fall back to its default label");
        Assert.AreNotEqual("Front camera", defaultName);
    }

    [STATestMethod]
    public void ScreenToImageIsNullWithoutAnImage()
    {
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        Assert.IsTrue(control.ApplyTemplate());
        Assert.IsNull(control.ScreenToImage(new System.Windows.Point(10, 10)));
    }

    [STATestMethod]
    public void StateChangedFollowsTheStateProperties()
    {
        // An app reacting to the state as a whole gets one event per change, after every property is current.
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        Assert.IsTrue(control.ApplyTemplate());
        var viewport = new FakeInnerViewport();
        control.SetActiveViewport(viewport);

        var log = new List<string>();
        foreach (DependencyProperty property in new[] { OrientedImageViewport.IsBusyProperty, OrientedImageViewport.IsInteractiveProperty, OrientedImageViewport.ErrorProperty })
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(property, typeof(OrientedImageViewport)).AddValueChanged(control, (_, _) => log.Add(property.Name));
        LogStateChanged(control, log);

        viewport.Publish(interactive: true);
        CollectionAssert.AreEqual(new[] { "IsInteractive", "StateChanged: interactive=True, error=none" }, log, "an image becomes interactive");

        log.Clear();
        viewport.Publish(interactive: true);
        Assert.IsEmpty(log, "an unchanged state raises nothing");

        viewport.Publish(interactive: true, error: new InvalidOperationException("render failed"));
        CollectionAssert.AreEqual(new[] { "IsInteractive", "Error", "StateChanged: interactive=False, error=render failed" }, log, "one event follows both property changes");

        log.Clear();
        control.SetActiveViewport(null);
        CollectionAssert.AreEqual(new[] { "Error", "StateChanged: interactive=False, error=none" }, log, "removing the inner viewport clears the error");
    }

    [STATestMethod]
    public void StateChangedReportsTheNewestStateWhenAHandlerClearsTheImage()
    {
        // The handler runs after IsInteractive is set and before Error is, so a stale write would restore the error.
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        Assert.IsTrue(control.ApplyTemplate());
        var viewport = new FakeInnerViewport();
        control.SetActiveViewport(viewport);
        viewport.Publish(interactive: true);
        OnceWhenNotInteractive(control, () => control.SetActiveViewport(null));
        var log = new List<string>();
        LogStateChanged(control, log);

        viewport.Publish(interactive: true, error: new InvalidOperationException("render failed"));

        CollectionAssert.AreEqual(new[] { "StateChanged: interactive=False, error=none" }, log);
    }

    [STATestMethod]
    public void StateChangedReportsTheNewestStateWhenAHandlerSwitchesImages()
    {
        // The handler moves on to an image that has failed too, so a stale write would report the first image's error.
        var control = new OrientedImageViewport { Template = CreateHostTemplate() };
        Assert.IsTrue(control.ApplyTemplate());
        var first = new FakeInnerViewport();
        control.SetActiveViewport(first);
        first.Publish(interactive: true);
        var second = new FakeInnerViewport();
        second.Publish(interactive: false, error: new InvalidOperationException("second failed"));
        OnceWhenNotInteractive(control, () => control.SetActiveViewport(second));
        var log = new List<string>();
        LogStateChanged(control, log);

        first.Publish(interactive: true, error: new InvalidOperationException("first failed"));

        CollectionAssert.AreEqual(new[] { "StateChanged: interactive=False, error=second failed" }, log);
    }

    // Runs the action from inside the control's state update, the first time IsInteractive turns false.
    private static void OnceWhenNotInteractive(OrientedImageViewport control, Action action)
    {
        bool done = false;
        System.ComponentModel.DependencyPropertyDescriptor.FromProperty(OrientedImageViewport.IsInteractiveProperty, typeof(OrientedImageViewport)).AddValueChanged(control, (_, _) =>
        {
            if (control.IsInteractive || done)
                return;

            done = true;
            action();
        });
    }

    private static void LogStateChanged(OrientedImageViewport control, List<string> log) =>
        control.StateChanged += (_, _) => log.Add($"StateChanged: interactive={control.IsInteractive}, error={control.Error?.Message ?? "none"}");

    // The active inner viewport's focusable view; here, the raster viewport's MapView.
    private static DependencyObject GetFocusableView(OrientedImageViewport control)
    {
        var view = GetHost(control).Child as DependencyObject;
        Assert.IsNotNull(view, "the host presents the active inner viewport's view");
        return view;
    }

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(3857, 3857)]
    [DataRow(3857, 0)]
    public void VisibleAreaIsClippedToImageNotClamped(int viewWkid, int extentWkid)
    {
        // The diamond view contains the whole 100 by 100 image. Clamping each vertex, rather than clipping, would give
        // the diamond of the edge midpoints, with half the area. The last row matches the raster viewport, where only
        // the visible area has a spatial reference.
        var builder = new Esri.ArcGISRuntime.Geometry.PolygonBuilder(viewWkid == 0 ? null : SpatialReference.Create(viewWkid));
        builder.AddPoint(50, 150);
        builder.AddPoint(150, 50);
        builder.AddPoint(50, -50);
        builder.AddPoint(-50, 50);
        var visibleArea = builder.ToGeometry();
        var extent = new Esri.ArcGISRuntime.Geometry.Envelope(0, 0, 100, 100, extentWkid == 0 ? null : SpatialReference.Create(extentWkid));

        List<System.Drawing.PointF> ring = OrientedImageRasterViewport.ComputeVisibleAreaPixels(visibleArea, extent, 1, 1);

        Assert.IsGreaterThanOrEqualTo(4, ring.Count, $"expected a full ring, got {ring.Count} vertices");
        Assert.AreEqual(10000d, SignedRingArea(ring), 1e-3, "the clipped footprint must cover the whole image clockwise");
    }

    // Shoelace area is positive for clockwise rings in image coordinates, where y increases downward.
    private static double SignedRingArea(IReadOnlyList<PointF> ring)
    {
        double twiceArea = 0;
        for (int index = 0; index < ring.Count; index++)
        {
            PointF current = ring[index];
            PointF next = ring[(index + 1) % ring.Count];
            twiceArea += ((double)current.X * next.Y) - ((double)next.X * current.Y);
        }

        return twiceArea / 2;
    }

    [TestMethod]
    [DataRow(1, 40f, 30f)]
    [DataRow(2, 360f, 30f)]
    [DataRow(3, 360f, 170f)]
    [DataRow(4, 40f, 170f)]
    [DataRow(5, 30f, 40f)]
    [DataRow(6, 170f, 40f)]
    [DataRow(7, 170f, 360f)]
    [DataRow(8, 30f, 360f)]
    public void ExifOrientationMapsStoredPixelsToDecodedPixels(int exifOrientation, float expectedX, float expectedY)
    {
        // A non-square image and an off-center point tell rotations from reflections. Two wrong inverses could still
        // round-trip, so the expected point is checked too.
        var orientation = new ExifOrientationTransform(exifOrientation);
        var stored = new PointF(40, 30);
        PointF image = orientation.StoredToImage(stored, 400, 200);

        Assert.AreEqual(new PointF(expectedX, expectedY), image);
        Assert.AreEqual(stored, orientation.ImageToStored(image, 400, 200));

        // The planar viewport can rotate but not reflect, so a clockwise rotation by RotationDegrees followed by an
        // optional horizontal mirror must reproduce the mapping.
        (PointF rotated, float rotatedWidth) = orientation.RotationDegrees switch
        {
            90 => (new PointF(200 - stored.Y, stored.X), 200f),
            180 => (new PointF(400 - stored.X, 200 - stored.Y), 400f),
            270 => (new PointF(stored.Y, 400 - stored.X), 200f),
            _ => (stored, 400f),
        };
        Assert.AreEqual(image, orientation.IsMirrored ? new PointF(rotatedWidth - rotated.X, rotated.Y) : rotated);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ExifOrientationReadsBothTiffByteOrders(bool littleEndian)
    {
        // Cameras write both byte orders, but WIC-written fixtures are always little-endian. The TIFF header puts IFD0
        // at offset 8; its one entry is Orientation (0x0112), type SHORT, count 1, value 6, followed by no next IFD.
        byte[] tiff = littleEndian
            ? [(byte)'I', (byte)'I', 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0]
            : [(byte)'M', (byte)'M', 0, 42, 0, 0, 0, 8, 0, 1, 0x01, 0x12, 0, 3, 0, 0, 0, 1, 0, 6, 0, 0, 0, 0, 0, 0];
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE1, 0, (byte)(2 + 6 + tiff.Length), .. "Exif\0\0"u8, .. tiff, 0xFF, 0xDA];

        using var stream = new MemoryStream(jpeg);
        Assert.AreEqual(new ExifOrientationTransform(6), ExifOrientationTransform.Read(stream));
    }

    [TestMethod]
    [DataRow(1, 400f, 200f)]
    [DataRow(2, 400f, 200f)]
    [DataRow(3, 400f, 200f)]
    [DataRow(4, 400f, 200f)]
    [DataRow(5, 200f, 400f)]
    [DataRow(6, 200f, 400f)]
    [DataRow(7, 200f, 400f)]
    [DataRow(8, 200f, 400f)]
    public void ExifFootprintUsesDecodedDimensionsAndClockwiseWinding(int exifOrientation, float expectedWidth, float expectedHeight)
    {
        // Reflection reverses winding, and the SDK requires clockwise rings.
        var orientation = new ExifOrientationTransform(exifOrientation);
        var builder = new PolygonBuilder((SpatialReference?)null);
        builder.AddPoint(0, 200);
        builder.AddPoint(400, 200);
        builder.AddPoint(400, 0);
        builder.AddPoint(0, 0);
        List<PointF> pixels = OrientedImageRasterViewport.ComputeVisibleAreaPixels(
            builder.ToGeometry(), new Envelope(0, 0, 400, 200), 1, 1, orientation);

        PointF[] expectedCorners =
        [
            new(0, 0),
            new(expectedWidth, 0),
            new(expectedWidth, expectedHeight),
            new(0, expectedHeight),
        ];

        CollectionAssert.AreEquivalent(expectedCorners, pixels.Distinct().ToArray());
        Assert.AreEqual(80000d, SignedRingArea(pixels), "the full image must be covered with clockwise winding");

        var partialExtent = new Envelope(100, 200, 900, 800);
        var partialView = new Polygon([new MapPoint(-300, 1100), new(500, 1100), new(500, 500), new(-300, 500)]);
        List<PointF> partial = OrientedImageRasterViewport.ComputeVisibleAreaPixels(partialView, partialExtent, -2, -3, orientation);
        PointF[] storedCorners = [new(0, 0), new(200, 0), new(200, 100), new(0, 100)];
        CollectionAssert.AreEquivalent(storedCorners.Select(point => orientation.StoredToImage(point, 400, 200)).ToArray(), partial.Distinct().ToArray());
        Assert.AreEqual(20000d, SignedRingArea(partial), "partial clips keep clockwise image winding");
        Assert.AreEqual(120000d, SignedRingArea(OrientedImageRasterViewport.ComputeVisibleAreaPixels(partialView, partialExtent, 0, 0, orientation)), "zero cell sizes use one map unit per pixel");

        var outsideView = new Polygon([new MapPoint(-300, 1100), new(0, 1100), new(0, 900), new(-300, 900)]);
        var multipart = new PolygonBuilder(outsideView);
        multipart.AddPart(partialView.Parts[0]);
        Assert.IsEmpty(OrientedImageRasterViewport.ComputeVisibleAreaPixels(multipart.ToGeometry(), partialExtent, 0, 0, orientation), "only the first visible-area part counts");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    public async Task PanoramicJpegDecodeMatchesSdkOrientation(int exifOrientation)
    {
        // Only the EXIF tag varies. RuntimeImage gives the SDK's decoded pixels, which the panorama decoder must match.
        const int StoredWidth = 60;
        const int StoredHeight = 40;
        string path = Path.Combine(Path.GetTempPath(), $"toolkit-exif-{Guid.NewGuid():N}.jpg");
        try
        {
            WriteExifJpeg(path, exifOrientation, StoredWidth, StoredHeight);
            var uri = new Uri(path);
            var storedPoint = new PointF(10, 15);
            var expectedOrientation = new ExifOrientationTransform(exifOrientation);
            ExifOrientationTransform parsedOrientation = ExifOrientationTransform.Read(path);
            Assert.AreEqual(
                expectedOrientation.StoredToImage(storedPoint, StoredWidth, StoredHeight),
                parsedOrientation.StoredToImage(storedPoint, StoredWidth, StoredHeight),
                "the JPEG reader must recover the orientation written to the fixture");

            var expected = new RuntimeImage(uri);
            PanoramaFrame actual = await PanoramaFrame.DecodeAsync(path, CancellationToken.None);
            Assert.AreEqual(expected.Width, actual.Width);
            Assert.AreEqual(expected.Height, actual.Height);
            await AssertDecodedPixelsMatchAsync(expected, actual, exifOrientation);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void WriteExifJpeg(string path, int exifOrientation, int width, int height)
    {
        // Four distinct quadrants reveal flips and rotations.
        const int BytesPerPixel = 4;
        Color[] quadrantColors = [Color.Red, Color.Yellow, Color.Blue, Color.Cyan];
        byte[] pixels = new byte[width * height * BytesPerPixel];
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int quadrant = (row < height / 2 ? 0 : 2) + (column < width / 2 ? 0 : 1);
                Color color = quadrantColors[quadrant];
                int offset = ((row * width) + column) * BytesPerPixel;
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = color.A;
            }
        }

        var metadata = new BitmapMetadata("jpg");
        metadata.SetQuery("/app1/ifd/{ushort=274}", (ushort)exifOrientation);
        var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * BytesPerPixel);
        var encoder = new JpegBitmapEncoder { QualityLevel = 100 };
        encoder.Frames.Add(BitmapFrame.Create(source, null, metadata, null));
        using Stream output = File.Create(path);
        encoder.Save(output);
    }

    private static async Task AssertDecodedPixelsMatchAsync(RuntimeImage expected, PanoramaFrame actual, int exifOrientation)
    {
        using Stream raw = await expected.GetRawBufferAsync();
        using var buffer = new MemoryStream();
        await raw.CopyToAsync(buffer);
        byte[] expectedPixels = buffer.ToArray();

        // Sample inside each quadrant, away from edges, since WIC and the SDK round JPEG decoding slightly differently.
        const int BytesPerPixel = 4;
        const int ChannelTolerance = 3;
        int[] sampleRows = [actual.Height / 4, actual.Height * 3 / 4];
        int[] sampleColumns = [actual.Width / 4, actual.Width * 3 / 4];
        foreach (int row in sampleRows)
        {
            foreach (int column in sampleColumns)
            {
                int offset = ((row * actual.Width) + column) * BytesPerPixel;
                for (int channel = 0; channel < BytesPerPixel; channel++)
                {
                    int difference = Math.Abs(expectedPixels[offset + channel] - actual.Bgra[offset + channel]);
                    Assert.IsLessThanOrEqualTo(ChannelTolerance, difference, $"EXIF {exifOrientation}, ({column}, {row}), channel {channel}");
                }
            }
        }
    }

    // A single named host border, like the default template.
    private static ControlTemplate CreateHostTemplate() => new(typeof(OrientedImageViewport))
    {
        VisualTree = new FrameworkElementFactory(typeof(Border), "PART_DisplayHost"),
    };

    private static Border GetHost(OrientedImageViewport control)
    {
        var host = control.Template.FindName("PART_DisplayHost", control) as Border;
        Assert.IsNotNull(host, "the applied template must contain PART_DisplayHost");
        return host;
    }

    // An inner viewport whose state the test sets directly, without loading an image.
    private sealed class FakeInnerViewport : OrientedImageInnerViewport
    {
        private bool _interactive;

        public override FrameworkElement View { get; } = new Border();

        protected override bool IsPresentationInteractive => _interactive;

        public void Publish(bool interactive, Exception? error = null)
        {
            _interactive = interactive;
            PresentationError = error;
            UpdateState();
        }

        public override void SetBackgroundColor(Color color)
        {
        }

        public override PointF? ScreenToImage(double x, double y) => null;

        protected override Task PresentAsync(OrientedImage image, string path, CancellationToken token) => Task.CompletedTask;

        protected override bool TryLookAt(LookAtTarget target) => false;

        protected override void ClearPresentation()
        {
        }

        protected override void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed)
        {
        }

        protected override void OnMarkerChanged(OrientedImageMarker marker, string? propertyName)
        {
        }

        protected override void OnAutoUpdateFootprintChanged(bool enabled)
        {
        }

        protected override Task? BeginFootprintUpdate(OrientedImageFootprint footprint) => null;
    }
}
