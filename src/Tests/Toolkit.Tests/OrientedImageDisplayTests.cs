using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
/// Contracts of <see cref="OrientedImageDisplay"/> and its inner displays that hold without a running app:
/// template re-hosting, marker subscriptions that must not retain a discarded display, marker offsets and hit-testing,
/// accessibility, visible-area clipping, and EXIF coordinate and decoder agreement.
/// </summary>
[TestClass]
public sealed class OrientedImageDisplayTests
{
    [TestMethod]
    public void ReapplyingTemplateRehostsActiveDisplay()
    {
        // A template replacement must move the existing display out of the discarded presenter.
        // Recreating the display would lose its current image and navigation state.
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
        // The application keeps the collection, marker, and symbol alive after discarding the display.
        // Their event subscriptions must not prevent the display from being collected.
        RunSta(() =>
        {
            var markers = new ObservableCollection<OrientedImageMarker>
            {
                new(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0)), new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10)),
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
    public void MarkerChangesStartOnlyTheNeededPasses()
    {
        RunSta(() =>
        {
            // Only drawn properties start a pass. A change inside the symbol counts, since the SDK symbol is what is
            // drawn. A replaced or removed marker's symbol is let go.
            var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10);
            var marker = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0)), symbol);
            var display = new OrientedImagePanoramicDisplay();
            display.SetMarkers(new[] { marker });
            int passes = display.MarkerGeneration;

            marker.Tag = "changed";
            Assert.AreEqual(passes, display.MarkerGeneration, "Tag is not drawn");

            symbol.Color = Color.Blue;
            Assert.AreEqual(++passes, display.MarkerGeneration, "a symbol change");

            marker.IsVisible = false;
            Assert.AreEqual(++passes, display.MarkerGeneration, "a visibility change");

            marker.Position = OrientedImageMarkerPosition.FromLocation(new MapPoint(1, 1));
            Assert.AreEqual(++passes, display.MarkerGeneration, "a position change");

            var replacement = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, Color.Red, 10);
            marker.Symbol = replacement;
            Assert.AreEqual(++passes, display.MarkerGeneration, "a symbol replacement");
            symbol.Color = Color.Green;
            Assert.AreEqual(passes, display.MarkerGeneration, "the replaced symbol is no longer watched");
            replacement.Color = Color.Green;
            Assert.AreEqual(++passes, display.MarkerGeneration, "the new symbol is watched");

            display.SetMarkers(null);
            passes = display.MarkerGeneration;
            replacement.Color = Color.Blue;
            Assert.AreEqual(passes, display.MarkerGeneration, "a removed marker's symbol is no longer watched");
        });
    }

    [TestMethod]
    public void BurstOfMarkerChangesTakesOnePass()
    {
        RunSta(() =>
        {
            // A change to a symbol that many markers share, or a loop over the markers, runs one pass.
            var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10);
            List<OrientedImageMarker> markers = Enumerable.Range(0, 50)
                .Select(i => new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(i, 0)), symbol))
                .ToList();
            var display = new OrientedImagePanoramicDisplay();
            display.SetMarkers(markers);
            RunPendingDispatcherWork();
            int passes = display.MarkerPasses;

            symbol.Color = Color.Blue;
            RunPendingDispatcherWork();
            Assert.AreEqual(passes + 1, display.MarkerPasses, "a shared symbol change");

            foreach (OrientedImageMarker marker in markers)
                marker.IsVisible = false;
            RunPendingDispatcherWork();
            Assert.AreEqual(passes + 2, display.MarkerPasses, "a loop over the markers");
        });
    }

    // Runs work posted to this thread's dispatcher, which has a higher priority than Background.
    private static void RunPendingDispatcherWork() =>
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { }));

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

        // A 10-DIP marker drawn 15 DIPs right of and 20 DIPs above its anchor.
        var offset = Marker(size: 10, offsetX: 15, offsetY: -20);
        Assert.AreSame(offset.Marker, Hit([offset], cx + 15, cy - 20), "a tap on the drawn marker hits it");
        Assert.IsNull(Hit([offset], cx, cy), "a tap on the bare anchor, 18 DIPs from the drawn marker, misses");

        // The whole symbol counts, plus 12 DIPs around it.
        var large = Marker(size: 80, offsetX: 0, offsetY: 0);
        Assert.IsNotNull(Hit([large], cx + 30, cy), "inside the symbol");
        Assert.IsNotNull(Hit([large], cx + 51, cy), "11 DIPs outside its edge");
        Assert.IsNull(Hit([large], cx + 55, cy), "15 DIPs outside its edge");

        // As in the planar display, the topmost marker within tolerance wins, even over a direct hit beneath it.
        var upper = Marker(size: 20, offsetX: 55, offsetY: 0);
        Assert.AreSame(upper.Marker, Hit([large, upper], cx + 38, cy), "inside the lower marker, 7 DIPs from the upper one");
        Assert.AreSame(large.Marker, Hit([large, upper], cx - 20, cy), "out of the upper marker's reach");

        OrientedImageMarker? Hit(OrientedImagePanoramicDisplay.ResolvedMarker[] markers, double x, double y) =>
            OrientedImagePanoramicDisplay.HitTestMarker(markers, camera, 400, 300, x, y);
    }

    [TestMethod]
    public void StillImagesSpanning360DegreesArePanoramic()
    {
        // The SDK's image transforms treat any image that spans 360 degrees as spherical.
        Assert.IsTrue(OrientedImageDisplay.IsPanoramic(OrientedImageType.Image360, new Dictionary<string, object?>()));
        Assert.IsTrue(OrientedImageDisplay.IsPanoramic(OrientedImageType.Unknown, new Dictionary<string, object?> { ["HorizontalFieldOfView"] = 360d }));
        Assert.IsFalse(OrientedImageDisplay.IsPanoramic(OrientedImageType.Horizontal, new Dictionary<string, object?> { ["HorizontalFieldOfView"] = 100.4d }));
    }

    [TestMethod]
    public void PanoramicHeadingTreatsUnknownAsNorth()
    {
        // -999 means the heading is unknown.
        Assert.AreEqual(0f, OrientedImagePanoramicDisplay.ReadHeadingRadians(new Dictionary<string, object?> { ["CameraHeading"] = -999d }));
        Assert.AreEqual(0f, OrientedImagePanoramicDisplay.ReadHeadingRadians(new Dictionary<string, object?>()));
        Assert.AreEqual(MathF.PI / 2f, OrientedImagePanoramicDisplay.ReadHeadingRadians(new Dictionary<string, object?> { ["CameraHeading"] = 90d }), 1e-6f);
    }

    // A marker at the image center with a square swatch of the given size in DIPs.
    private static OrientedImagePanoramicDisplay.ResolvedMarker Marker(double size, double offsetX, double offsetY) =>
        new(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(new MapPoint(0, 0))), 0.75f, 0.5f, offsetX, offsetY, size / 2, size / 2);

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
    public void AutomationPropertiesOnControlReachFocusableView()
    {
        RunSta(() =>
        {
            // Focus lands on the inner view, so the name and automation id the app gives the control must reach that
            // view, whether set before or after the view exists, and clearing the name must restore the default label.
            var control = new OrientedImageDisplay { Template = CreateHostTemplate() };
            System.Windows.Automation.AutomationProperties.SetName(control, "Rear camera");
            System.Windows.Automation.AutomationProperties.SetAutomationId(control, "RearCamera");
            Assert.IsTrue(control.ApplyTemplate());
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
        });
    }

    [TestMethod]
    public void ScreenToImageIsNullWithoutAnImage()
    {
        RunSta(() =>
        {
            var control = new OrientedImageDisplay { Template = CreateHostTemplate() };
            Assert.IsTrue(control.ApplyTemplate());
            Assert.IsNull(control.ScreenToImage(new System.Windows.Point(10, 10)));
        });
    }

    // The keyboard-focusable element of the active display: the MapView of the raster display here.
    private static DependencyObject GetFocusableView(OrientedImageDisplay control)
    {
        var display = GetHost(control).Content as ContentControl;
        Assert.IsNotNull(display, "the host presents the active display");
        var view = display.Content as DependencyObject;
        Assert.IsNotNull(view, "the display presents its focusable view");
        return view;
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

        Assert.IsGreaterThanOrEqualTo(4, ring.Count, $"expected a full ring, got {ring.Count} vertices");
        Assert.AreEqual(10000d, Math.Abs(SignedRingArea(ring)), 1e-3, "the clipped footprint must cover the whole image");
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
        // A non-square image and an off-center point distinguish rotations from reflections.
        // Check the expected coordinate as well as the round-trip: two incorrect inverses could still round-trip.
        var orientation = new ExifOrientationTransform(exifOrientation);
        var stored = new PointF(40, 30);
        PointF image = orientation.StoredToImage(stored, 400, 200);

        Assert.AreEqual(new PointF(expectedX, expectedY), image);
        Assert.AreEqual(stored, orientation.ImageToStored(image, 400, 200));

        // The planar display can rotate but not reflect, so the reflection must come last. Rotating clockwise by
        // RotationDegrees, then reflecting horizontally if mirrored, must reproduce the mapping.
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
        // A full-image footprint must reach the decoded grid's corners, including swapped dimensions.
        // Reflection reverses winding, so the output must restore the clockwise order required by the SDK.
        var orientation = new ExifOrientationTransform(exifOrientation);
        var builder = new PolygonBuilder((SpatialReference?)null);
        builder.AddPoint(0, 200);
        builder.AddPoint(400, 200);
        builder.AddPoint(400, 0);
        builder.AddPoint(0, 0);
        List<PointF> pixels = OrientedImageRasterDisplay.ComputeVisibleAreaPixels(
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
        // Keep the stored pixels unchanged and vary only the EXIF tag, exercising the real metadata reader.
        // RuntimeImage supplies the SDK's decoded pixel space; the panorama decoder must agree with it.
        const int StoredWidth = 60;
        const int StoredHeight = 40;
        string path = Path.Combine(Path.GetTempPath(), $"toolkit-exif-{Guid.NewGuid():N}.jpg");
        try
        {
            WriteExifJpeg(path, exifOrientation, StoredWidth, StoredHeight);
            var uri = new Uri(path);
            var storedPoint = new PointF(10, 15);
            var expectedOrientation = new ExifOrientationTransform(exifOrientation);
            ExifOrientationTransform parsedOrientation = ExifOrientationTransform.Read(uri);
            Assert.AreEqual(
                expectedOrientation.StoredToImage(storedPoint, StoredWidth, StoredHeight),
                parsedOrientation.StoredToImage(storedPoint, StoredWidth, StoredHeight),
                "the JPEG reader must recover the orientation written to the fixture");

            var expected = new RuntimeImage(uri);
            OrientedImagePanoramicDisplay.PanoramaFrame actual = await OrientedImagePanoramicDisplay.DecodeAsync(uri, CancellationToken.None);
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
        // Four distinct quadrants reveal flips and rotations that a uniform or symmetric image would hide.
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

    private static async Task AssertDecodedPixelsMatchAsync(RuntimeImage expected, OrientedImagePanoramicDisplay.PanoramaFrame actual, int exifOrientation)
    {
        using Stream raw = await expected.GetRawBufferAsync();
        using var buffer = new MemoryStream();
        await raw.CopyToAsync(buffer);
        byte[] expectedPixels = buffer.ToArray();

        // Sample inside each quadrant, away from JPEG edges. WIC and the SDK can differ slightly in JPEG rounding.
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
