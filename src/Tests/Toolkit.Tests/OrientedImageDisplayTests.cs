using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using PointF = System.Drawing.PointF;

namespace Toolkit.Tests;

/// <summary>
/// Hosting and lifetime contracts of <see cref="OrientedImageDisplay"/> and its inner displays that hold without
/// a running app: re-applying the control template must re-host the active display in the new template's host,
/// and app-owned markers must not retain a discarded display through their PropertyChanged subscriptions.
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
                new(OrientedImageMarkerPosition.FromImagePoint(new PointF(10f, 10f))),
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
    public void ReapplyingTemplateDoesNotRestartPresentation()
    {
        RunSta(() =>
        {
            var control = new OrientedImageDisplay { Template = CreateHostTemplate() };
            Assert.IsTrue(control.ApplyTemplate());
            object? activeDisplay = GetNonPublicField(control, "_activeDisplay");
            Assert.IsNotNull(activeDisplay, "sanity: the first template application selects a display");
            object? initialSession = GetNonPublicField(activeDisplay, "_sessionCts");
            Assert.IsNotNull(initialSession, "sanity: the first template application starts a presentation session");

            control.Template = CreateHostTemplate();
            Assert.IsTrue(control.ApplyTemplate());

            // Re-templating must only re-host: a new session would recreate the raster layer / re-decode the
            // panorama (I/O, flicker) and cancel valid in-flight work for an unchanged footprint.
            Assert.AreSame(activeDisplay, GetNonPublicField(control, "_activeDisplay"));
            Assert.AreSame(
                initialSession,
                GetNonPublicField(activeDisplay, "_sessionCts"),
                "re-applying the template must not start a new presentation session");
        });
    }

    [TestMethod]
    public void UnsupportedTypeErrorPublishesWithoutAnActiveDisplay()
    {
        RunSta(() =>
        {
            var control = new OrientedImageDisplay { Template = CreateHostTemplate() };
            Assert.IsTrue(control.ApplyTemplate());

            // Arrange the boundary state UpdateDisplay produces when the first-ever footprint has an unsupported
            // image type (no display was ever active; a video-typed OrientedImage is not constructible headless):
            // the null -> null display transition must still publish the freshly computed unsupported-type error.
            SetNonPublicField(control, "_activeDisplay", null);
            var unsupported = new NotSupportedException("test: unsupported image type");
            SetNonPublicField(control, "_unsupportedError", unsupported);

            InvokeNonPublic(control, "SetActiveDisplay", [null]);

            Assert.AreSame(unsupported, control.Error, "the unsupported-type error must publish even when no display was ever active");
        });
    }

    [TestMethod]
    public void PanoramicRecoveryStateIsBusyAndNotInteractive()
    {
        RunSta(() =>
        {
            var display = new OrientedImagePanoramicDisplay();

            // Simulate a presented panorama (dimensions come from a real decode in production).
            SetNonPublicField(display, "_imageWidth", 4096);
            SetNonPublicField(display, "_imageHeight", 2048);
            InvokeNonPublic(display, "UpdateState", []);
            Assert.IsTrue(display.IsInteractive, "sanity: presented and error-free implies interactive");
            Assert.IsFalse(display.IsBusy);

            // The device-recovery entry state: dimensions invalidated, recovery flagged, state recomputed. During
            // the re-decode the surface is blank; commands bound to IsInteractive must not stay enabled over it.
            SetNonPublicField(display, "_imageWidth", 0);
            SetNonPublicField(display, "_imageHeight", 0);
            SetNonPublicField(display, "_recovering", true);
            InvokeNonPublic(display, "UpdateState", []);

            Assert.IsFalse(display.IsInteractive, "a blank recovering surface must not be interactive");
            Assert.IsTrue(display.IsBusy, "device recovery is presentation work and must surface as busy");
        });
    }

    // The boundary tests arrange non-public state (real OrientedImages of specific types are not constructible
    // headless) - resolve members across the inheritance chain.
    private static object? GetNonPublicField(object target, string name) => GetFieldInfo(target, name).GetValue(target);

    private static void SetNonPublicField(object target, string name, object? value) => GetFieldInfo(target, name).SetValue(target, value);

    private static FieldInfo GetFieldInfo(object target, string name)
    {
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            if (type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo field)
                return field;
        }

        throw new MissingFieldException(target.GetType().Name, name);
    }

    private static void InvokeNonPublic(object target, string name, object?[] args)
    {
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            if (type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance) is MethodInfo method)
            {
                method.Invoke(target, args);
                return;
            }
        }

        throw new MissingMethodException(target.GetType().Name, name);
    }

    [TestMethod]
    public void ComputedStatePropertiesRejectExternalWrites()
    {
        RunSta(() =>
        {
            // IsBusy/IsInteractive/Error are control-owned computed state; on WPF they are registered read-only,
            // so an external SetValue cannot overwrite what the control computed.
            var control = new OrientedImageDisplay();
            Assert.ThrowsExactly<InvalidOperationException>(() => control.SetValue(OrientedImageDisplay.IsBusyProperty, true));
            Assert.ThrowsExactly<InvalidOperationException>(() => control.SetValue(OrientedImageDisplay.IsInteractiveProperty, true));
            Assert.ThrowsExactly<InvalidOperationException>(() => control.SetValue(OrientedImageDisplay.ErrorProperty, new InvalidOperationException("external")));
        });
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
