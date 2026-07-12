using System.Collections.ObjectModel;
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
