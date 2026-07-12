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
}
