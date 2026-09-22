using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Toolkit.Tests;

/// <summary>
/// Locks the panorama coordinate conventions documented on <see cref="PanoramaCameraState"/>.
/// Every platform renderer's sphere mesh and camera must agree with this math, so a failure here
/// means clicks/markers/footprints would be mirrored or offset on some platform.
/// </summary>
[TestClass]
public sealed class PanoramaCameraStateTests
{
    private const double ViewWidth = 800;
    private const double ViewHeight = 600;
    private const float Fov90 = MathF.PI / 2f;

    [TestMethod]
    public void ScreenCenterAtIdentityCameraMapsToConventionAnchor()
    {
        var camera = new PanoramaCameraState(yaw: 0f, pitch: 0f, fieldOfView: Fov90);

        Assert.IsTrue(camera.TryScreenToNormalizedUv(ViewWidth / 2, ViewHeight / 2, ViewWidth, ViewHeight, out float u, out float v));

        // The documented convention: identity camera looks at (0.75, 0.5).
        Assert.AreEqual(0.75f, u, 1e-4f);
        Assert.AreEqual(0.5f, v, 1e-4f);
    }

    [TestMethod]
    public void ScreenCenterTracksYawAndPitchLinearly()
    {
        // u_center = 0.75 + yaw/2pi (mod 1); v_center = 0.5 + pitch/pi.
        foreach (float yaw in new[] { -2.5f, -0.8f, 0f, 0.4f, 1.9f, 3.1f })
        {
            foreach (float pitch in new[] { -1.2f, -0.3f, 0f, 0.3f, 1.2f })
            {
                var camera = new PanoramaCameraState(yaw, pitch, Fov90);
                Assert.IsTrue(camera.TryScreenToNormalizedUv(ViewWidth / 2, ViewHeight / 2, ViewWidth, ViewHeight, out float u, out float v));

                float expectedU = 0.75f + (yaw / (2f * MathF.PI));
                expectedU -= MathF.Floor(expectedU); // mod 1
                float expectedV = 0.5f + (pitch / MathF.PI);

                Assert.AreEqual(expectedU, u, 1e-3f, $"u at yaw={yaw}, pitch={pitch}");
                Assert.AreEqual(expectedV, v, 1e-3f, $"v at yaw={yaw}, pitch={pitch}");
            }
        }
    }

    [TestMethod]
    public void ScreenToUvRoundTripsAcrossCameraAndScreenGrid()
    {
        float[] yaws = [-2.5f, -1f, 0f, 0.7f, 2f, 3.1f];
        float[] pitches = [-1.2f, -0.5f, 0f, 0.5f, 1.2f];
        float[] fovs = [PanoramaCameraState.MinFieldOfView, Fov90, PanoramaCameraState.MaxFieldOfView];

        foreach (float yaw in yaws)
        {
            foreach (float pitch in pitches)
            {
                foreach (float fov in fovs)
                {
                    var camera = new PanoramaCameraState(yaw, pitch, fov);
                    for (int ix = 0; ix <= 4; ix++)
                    {
                        for (int iy = 0; iy <= 4; iy++)
                        {
                            double x = (0.1 + (0.2 * ix)) * ViewWidth;
                            double y = (0.1 + (0.2 * iy)) * ViewHeight;

                            string context = $"yaw={yaw}, pitch={pitch}, fov={fov}, screen=({x},{y})";
                            Assert.IsTrue(camera.TryScreenToNormalizedUv(x, y, ViewWidth, ViewHeight, out float u, out float v), $"screen->uv failed: {context}");
                            Assert.IsTrue(u is >= 0f and <= 1f, $"u out of range ({u}): {context}");
                            Assert.IsTrue(v is >= 0f and <= 1f, $"v out of range ({v}): {context}");

                            Assert.IsTrue(camera.TryNormalizedUvToScreen(u, v, ViewWidth, ViewHeight, out double backX, out double backY), $"uv->screen failed: {context}");
                            Assert.AreEqual(x, backX, 0.1, $"x round-trip: {context}");
                            Assert.AreEqual(y, backY, 0.1, $"y round-trip: {context}");
                        }
                    }
                }
            }
        }
    }

    [TestMethod]
    public void ScreenToUvRoundTripsOnNonSquareViewports()
    {
        // Non-square aspect ratios exercise the width/height mapping independently.
        foreach ((double w, double h) in new[] { (800.0, 400.0), (300.0, 900.0), (1024.0, 768.0) })
        {
            var camera = new PanoramaCameraState(yaw: 0.6f, pitch: -0.4f, fieldOfView: Fov90);

            double x = 0.3 * w;
            double y = 0.7 * h;
            Assert.IsTrue(camera.TryScreenToNormalizedUv(x, y, w, h, out float u, out float v));
            Assert.IsTrue(camera.TryNormalizedUvToScreen(u, v, w, h, out double backX, out double backY));
            Assert.AreEqual(x, backX, 0.1, $"viewport {w}x{h}");
            Assert.AreEqual(y, backY, 0.1, $"viewport {w}x{h}");
        }
    }

    [TestMethod]
    public void UvToScreenBehindCameraReturnsFalse()
    {
        var camera = new PanoramaCameraState(yaw: 0f, pitch: 0f, fieldOfView: Fov90);

        // The antipode of the view center (0.75, 0.5) is directly behind the camera.
        Assert.IsFalse(camera.TryNormalizedUvToScreen(0.25f, 0.5f, ViewWidth, ViewHeight, out _, out _));
    }

    [TestMethod]
    public void TransformsWithInvalidViewportReturnFalse()
    {
        var camera = new PanoramaCameraState(yaw: 0f, pitch: 0f, fieldOfView: Fov90);

        Assert.IsFalse(camera.TryScreenToNormalizedUv(0, 0, 0, ViewHeight, out _, out _));
        Assert.IsFalse(camera.TryScreenToNormalizedUv(0, 0, ViewWidth, 0, out _, out _));
        Assert.IsFalse(camera.TryNormalizedUvToScreen(0.5f, 0.5f, 0, ViewHeight, out _, out _));
        Assert.IsFalse(camera.TryNormalizedUvToScreen(0.5f, 0.5f, ViewWidth, 0, out _, out _));
    }

    [TestMethod]
    public void YawOfNegativeQuarterTurnCentersUvHalf()
    {
        // The identity camera centers u = 0.75, so re-anchoring the view to the image center (u = 0.5, the column
        // that faces the camera heading) takes yaw = -pi/2. The panoramic display's initial view builds on this:
        // yaw = -pi/2 - heading looks north, since azimuth(u) = heading + (u - 0.5) * 2pi.
        var camera = new PanoramaCameraState(yaw: -MathF.PI / 2f, pitch: 0f, fieldOfView: Fov90);

        Assert.IsTrue(camera.TryScreenToNormalizedUv(ViewWidth / 2, ViewHeight / 2, ViewWidth, ViewHeight, out float u, out float v));
        Assert.AreEqual(0.5f, u, 1e-4f);
        Assert.AreEqual(0.5f, v, 1e-4f);
    }

    [TestMethod]
    public void TransformsRejectNonFiniteInputs()
    {
        // NaN passes every comparison in the projection math (all comparisons with NaN are false), so without
        // explicit guards both transforms would "succeed" and hand NaN coordinates to markers and GPU vertices.
        var camera = new PanoramaCameraState(yaw: 0.3f, pitch: 0.1f, fieldOfView: Fov90);

        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.IsFalse(camera.TryNormalizedUvToScreen(bad, 0.5f, ViewWidth, ViewHeight, out double sx, out double sy), $"u={bad}");
            Assert.AreEqual(0d, sx, $"screenX must stay at its failure value for u={bad}");
            Assert.AreEqual(0d, sy, $"screenY must stay at its failure value for u={bad}");
            Assert.IsFalse(camera.TryNormalizedUvToScreen(0.75f, bad, ViewWidth, ViewHeight, out _, out _), $"v={bad}");
        }

        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.IsFalse(camera.TryScreenToNormalizedUv(bad, ViewHeight / 2, ViewWidth, ViewHeight, out float u, out float v), $"x={bad}");
            Assert.AreEqual(0f, u, $"u must stay at its failure value for x={bad}");
            Assert.AreEqual(0f, v, $"v must stay at its failure value for x={bad}");
            Assert.IsFalse(camera.TryScreenToNormalizedUv(ViewWidth / 2, bad, ViewWidth, ViewHeight, out _, out _), $"y={bad}");
        }
    }

    [TestMethod]
    public void DragRotationScaleMatchesFormulaAndFallsBackWhenSizeUnknown()
    {
        // 2 * tan(fov/2) / height: at fov=90deg, height=1000 DIP -> 0.002 rad/DIP.
        Assert.AreEqual(0.002f, PanoramaCameraState.DragRotationScale(Fov90, 1000), 1e-6f);

        // Wider FOV or shorter viewport rotates faster per DIP.
        Assert.IsGreaterThan(PanoramaCameraState.DragRotationScale(PanoramaCameraState.MinFieldOfView, 1000), PanoramaCameraState.DragRotationScale(PanoramaCameraState.MaxFieldOfView, 1000));
        Assert.IsGreaterThan(PanoramaCameraState.DragRotationScale(Fov90, 1000), PanoramaCameraState.DragRotationScale(Fov90, 500));

        // Defensive fallback before the view has a size.
        Assert.AreEqual(PanoramaCameraState.MouseRotationScale, PanoramaCameraState.DragRotationScale(Fov90, 0));
        Assert.AreEqual(PanoramaCameraState.MouseRotationScale, PanoramaCameraState.DragRotationScale(Fov90, -5));
    }

    [TestMethod]
    public void FootprintViewAtIdentityCameraIsAQuarterTurnFromTheImageCenter()
    {
        // The identity camera looks at u = 0.75, a quarter turn clockwise from the image's center column (u = 0.5),
        // on the horizon; a square viewport spans the same angle horizontally and vertically.
        var camera = new PanoramaCameraState(yaw: 0f, pitch: 0f, fieldOfView: Fov90);

        Assert.IsTrue(camera.TryGetFootprintView(600, 600, out PanoramaCameraState.FootprintView view));

        Assert.AreEqual(90.0, view.Yaw, 1e-4);
        Assert.AreEqual(90.0, view.Pitch, 1e-4);
        Assert.AreEqual(90.0, view.VerticalFieldOfView, 1e-4);
        Assert.AreEqual(90.0, view.HorizontalFieldOfView, 1e-4);
    }

    [TestMethod]
    public void FootprintViewMatchesTheProjectedViewCenter()
    {
        // The footprint convention maps image column u = 0.5 + yaw/360 and row v = 1 - pitch/180, so the
        // orientation must agree with where the render projection puts the screen center.
        foreach (float yaw in new[] { -2.5f, -0.8f, 0f, 0.4f, 1.9f, 3.1f, 7f })
        {
            foreach (float pitch in new[] { -1.2f, -0.3f, 0f, 0.3f, 1.2f })
            {
                var camera = new PanoramaCameraState(yaw, pitch, Fov90);
                Assert.IsTrue(camera.TryGetFootprintView(ViewWidth, ViewHeight, out PanoramaCameraState.FootprintView view));
                Assert.IsTrue(camera.TryScreenToNormalizedUv(ViewWidth / 2, ViewHeight / 2, ViewWidth, ViewHeight, out float u, out float v));

                double expectedYaw = ((u - 0.5) * 360.0 + 720.0) % 360.0;
                double expectedPitch = (1.0 - v) * 180.0;
                Assert.AreEqual(expectedYaw, view.Yaw % 360.0, 0.5, $"yaw at yaw={yaw}, pitch={pitch}");
                Assert.AreEqual(expectedPitch, view.Pitch, 0.5, $"pitch at yaw={yaw}, pitch={pitch}");

                // Closed form: yaw = 90 + Yaw, pitch = 90 - Pitch (degrees), yaw wrapped into [0, 360).
                double closedFormYaw = ((90.0 + (yaw * 180.0 / Math.PI)) % 360.0 + 360.0) % 360.0;
                Assert.AreEqual(closedFormYaw, view.Yaw, 1e-3, $"closed-form yaw at yaw={yaw}");
                Assert.AreEqual(90.0 - (pitch * 180.0 / Math.PI), view.Pitch, 1e-3, $"closed-form pitch at pitch={pitch}");
                Assert.IsTrue(view.Yaw >= 0 && view.Yaw < 360, $"yaw range at yaw={yaw}: {view.Yaw}");
            }
        }
    }

    [TestMethod]
    public void FootprintViewOfTheInitialViewFacesNorth()
    {
        // The display starts a panorama looking north with Yaw = -pi/2 - CameraHeading; on the ground that view
        // direction is CameraHeading + yaw, which must come back to north.
        foreach (double headingDegrees in new[] { 0.0, 30.0, 90.0, 200.0, 359.0 })
        {
            float yaw = (float)((-Math.PI / 2) - (headingDegrees * Math.PI / 180));
            var camera = new PanoramaCameraState(yaw, 0f, Fov90);

            Assert.IsTrue(camera.TryGetFootprintView(ViewWidth, ViewHeight, out PanoramaCameraState.FootprintView view));

            double bearing = (headingDegrees + view.Yaw) % 360.0;
            Assert.IsTrue(bearing < 0.01 || bearing > 359.99, $"heading {headingDegrees}: view bearing {bearing}");
            Assert.AreEqual(90.0, view.Pitch, 1e-4);
        }
    }

    [TestMethod]
    public void FootprintViewHorizontalFovFollowsTheAspectRatioAndTheProjection()
    {
        // A perspective frustum with vertical FOV f spans 2*atan(aspect * tan(f/2)) horizontally: 106.26 degrees
        // for 90 degrees on 4:3, and the render projection must put the horizontal edge midpoint at that half angle.
        var camera = new PanoramaCameraState(0f, 0f, Fov90);

        Assert.IsTrue(camera.TryGetFootprintView(ViewWidth, ViewHeight, out PanoramaCameraState.FootprintView view));
        Assert.AreEqual(106.26, view.HorizontalFieldOfView, 0.01);
        Assert.AreEqual(90.0, view.VerticalFieldOfView, 1e-4);

        Assert.IsTrue(camera.TryScreenToNormalizedUv(ViewWidth / 2, ViewHeight / 2, ViewWidth, ViewHeight, out float centerU, out _));
        Assert.IsTrue(camera.TryScreenToNormalizedUv(0, ViewHeight / 2, ViewWidth, ViewHeight, out float leftU, out _));
        double halfSpan = ((centerU - leftU) * 360.0 + 720.0) % 360.0;
        Assert.AreEqual(view.HorizontalFieldOfView / 2, halfSpan, 0.5);

        // Wider viewport, wider horizontal span; the vertical one is the camera's.
        Assert.IsTrue(camera.TryGetFootprintView(1600, 600, out PanoramaCameraState.FootprintView wide));
        Assert.IsGreaterThan(view.HorizontalFieldOfView, wide.HorizontalFieldOfView);
        Assert.AreEqual(view.VerticalFieldOfView, wide.VerticalFieldOfView, 1e-6);
    }

    [TestMethod]
    public void FootprintViewRejectsAnUnsizedViewportAndInvalidCameras()
    {
        var camera = new PanoramaCameraState(0f, 0f, Fov90);
        Assert.IsFalse(camera.TryGetFootprintView(0, ViewHeight, out _));
        Assert.IsFalse(camera.TryGetFootprintView(ViewWidth, -1, out _));

        Assert.IsFalse(new PanoramaCameraState(float.NaN, 0f, Fov90).TryGetFootprintView(ViewWidth, ViewHeight, out _));
        Assert.IsFalse(new PanoramaCameraState(0f, float.PositiveInfinity, Fov90).TryGetFootprintView(ViewWidth, ViewHeight, out _));
        Assert.IsFalse(new PanoramaCameraState(0f, 0f, 0f).TryGetFootprintView(ViewWidth, ViewHeight, out _));
        Assert.IsFalse(new PanoramaCameraState(0f, 0f, MathF.PI).TryGetFootprintView(ViewWidth, ViewHeight, out _));
    }
}
