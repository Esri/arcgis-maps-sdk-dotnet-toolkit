// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if WPF || WINDOWS_XAML || MAUI
using System;
using System.Numerics;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

// Platform-neutral inverse of the panorama render projection: screen point <-> normalized equirectangular (u,v) in
// [0,1]. Multiply (u,v) by the image pixel dimensions to reach the pixel space the OrientedImage transforms use.
// Every renderer's sphere mesh and camera matrices must match this convention, or taps and markers are mirrored:
//   - Sphere: unit sphere around the camera, x = sin(phi)*cos(theta), y = cos(phi), z = sin(phi)*sin(theta);
//     u = theta/2pi (wrap), v = phi/pi (clamp), so v = 0 is up and the horizon is v = 0.5.
//   - Camera: right-handed at the origin looking down -Z; world = RotationY(Yaw) * RotationX(Pitch) in the
//     System.Numerics row-vector convention. Angles are radians; FieldOfView is vertical. At Yaw = Pitch = 0 the view
//     center is (u,v) = (0.75, 0.5); u_center = 0.75 + Yaw/2pi (mod 1) and v_center = 0.5 + Pitch/pi (positive looks down).
//   - Screen: element/DIP coordinates, origin top-left, +Y down.
internal sealed record PanoramaCameraState
{
    private const float NearPlane = 0.1f;
    private const float FarPlane = 10f;

    public static readonly PanoramaCameraState Initial = new(0f, 0f, MathF.PI / 2f);

    // Shared camera limits and input tuning, used by every platform's camera/gesture layer.
    public const float MinPitch = -(MathF.PI / 2f) + 0.01f;
    public const float MaxPitch = (MathF.PI / 2f) - 0.01f;
    public const float MinFieldOfView = 50f * MathF.PI / 180f;
    public const float MaxFieldOfView = 120f * MathF.PI / 180f;
    public const float MouseRotationScale = 0.0035f; // fallback drag scale while the view size is unknown
    public const float WheelZoomStep = 0.1f; // field of view change per mouse wheel notch, in radians

    // Held-key navigation speeds, matching the SDK MapView: arrows move the view 300 DIPs per second on screen, and
    // zooming changes the scale by a factor of 2 per second.
    public const double KeyboardPanSpeed = 300;
    public const double KeyboardZoomRate = 2;

    // Caps the time step of a single keyboard frame, so a stalled frame doesn't make the view jump.
    public const double MaxKeyboardStepSeconds = 0.1;

    // The navigation keys currently held down, as polled by each platform's input layer every frame.
    [Flags]
    public enum NavigationKeys
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
        ZoomIn = 16,
        ZoomOut = 32,
    }

    // Rad-per-DIP drag scale so the grabbed point tracks the pointer at screen center regardless of control size,
    // zoom and DPI. The same factor serves yaw (the aspect's width cancels).
    public static float DragRotationScale(float fieldOfView, double viewHeight)
    {
        if (viewHeight <= 0)
            return MouseRotationScale; // defensive: dragging is not really reachable before size is known

        return (float)(2.0 * Math.Tan(fieldOfView / 2.0) / viewHeight);
    }

    public PanoramaCameraState(float yaw, float pitch, float fieldOfView)
    {
        Yaw = yaw;
        Pitch = pitch;
        FieldOfView = fieldOfView;
    }

    public float Yaw { get; init; }

    public float Pitch { get; init; }

    public float FieldOfView { get; init; }

    public PanoramaCameraState Drag(float dx, float dy, double viewHeight)
    {
        float scale = DragRotationScale(FieldOfView, viewHeight);
        return this with { Yaw = Yaw - (dx * scale), Pitch = Math.Clamp(Pitch - (dy * scale), MinPitch, MaxPitch) };
    }

    public PanoramaCameraState Zoom(float scale) =>
        this with { FieldOfView = Math.Clamp(FieldOfView / scale, MinFieldOfView, MaxFieldOfView) };

    // One wheel notch zooms in by WheelZoomStep; a precision touchpad or a high-resolution wheel sends fractions.
    public PanoramaCameraState ZoomWheel(float notches) =>
        this with { FieldOfView = Math.Clamp(FieldOfView - (notches * WheelZoomStep), MinFieldOfView, MaxFieldOfView) };

    // One frame of held-key navigation. Unlike a drag, which moves the image, an arrow moves the view toward its side,
    // as in MapView: Left looks left and Up looks up. Held keys combine, so Up+Left moves diagonally. viewHeight uses
    // the same units as DragRotationScale.
    public PanoramaCameraState Navigate(NavigationKeys keys, double seconds, double viewHeight)
    {
        float angle = (float)(KeyboardPanSpeed * seconds) * DragRotationScale(FieldOfView, viewHeight);
        int right = Axis(NavigationKeys.Right, NavigationKeys.Left);
        int down = Axis(NavigationKeys.Down, NavigationKeys.Up);
        int zoomIn = Axis(NavigationKeys.ZoomIn, NavigationKeys.ZoomOut);

        // Yaw turns the view right and Pitch turns it down (see the convention above).
        float yaw = Yaw + (right * angle);
        float pitch = Math.Clamp(Pitch + (down * angle), MinPitch, MaxPitch);

        // The view's scale is 1 / tan(FieldOfView / 2), so zooming in for a second doubles it, as it does in MapView.
        float fieldOfView = FieldOfView;
        if (zoomIn != 0)
        {
            double halfTangent = Math.Tan(FieldOfView / 2.0) * Math.Pow(KeyboardZoomRate, -zoomIn * seconds);
            fieldOfView = Math.Clamp((float)(2.0 * Math.Atan(halfTangent)), MinFieldOfView, MaxFieldOfView);
        }

        return new PanoramaCameraState(yaw, pitch, fieldOfView);

        // +1, -1, or 0 when both or neither of an opposing key pair are held.
        int Axis(NavigationKeys positive, NavigationKeys negative) =>
            (keys.HasFlag(positive) ? 1 : 0) - (keys.HasFlag(negative) ? 1 : 0);
    }

    // Screen point -> normalized (u,v): the inverse of the render's world*projection.
    public bool TryScreenToNormalizedUv(double screenX, double screenY, double viewWidth, double viewHeight, out float u, out float v)
    {
        u = 0f;
        v = 0f;
        if (viewWidth <= 0 || viewHeight <= 0)
            return false;

        // Reject non-finite inputs: NaN passes every comparison below (finite inputs keep the math finite).
        if (!double.IsFinite(screenX) || !double.IsFinite(screenY))
            return false;

        if (!Matrix4x4.Invert(GetWorldViewProjection((float)(viewWidth / viewHeight)), out Matrix4x4 inverse))
            return false;

        var clip = new Vector4((float)(((screenX / viewWidth) * 2.0) - 1.0), (float)(1.0 - ((screenY / viewHeight) * 2.0)), 1f, 1f);
        Vector4 local = Vector4.Transform(clip, inverse);
        if (MathF.Abs(local.W) <= float.Epsilon)
            return false;

        Vector3 ray = Vector3.Normalize(new Vector3(local.X / local.W, local.Y / local.W, local.Z / local.W));
        float phi = MathF.Acos(Math.Clamp(ray.Y, -1f, 1f));
        float theta = MathF.Atan2(ray.Z, ray.X);
        if (theta < 0f)
            theta += 2f * MathF.PI;

        u = theta / (2f * MathF.PI);
        v = phi / MathF.PI;
        return true;
    }

    // Normalized (u,v) -> screen point; false when the point is behind the camera.
    public bool TryNormalizedUvToScreen(float u, float v, double viewWidth, double viewHeight, out double screenX, out double screenY)
    {
        screenX = 0;
        screenY = 0;
        if (viewWidth <= 0 || viewHeight <= 0)
            return false;

        // Reject non-finite inputs: NaN passes every comparison below, including the behind-camera test.
        if (!float.IsFinite(u) || !float.IsFinite(v))
            return false;

        float phi = v * MathF.PI;
        float theta = u * 2f * MathF.PI;
        var point = new Vector4(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta), 1f);
        Vector4 clip = Vector4.Transform(point, GetWorldViewProjection((float)(viewWidth / viewHeight)));
        if (clip.W <= 0f)
            return false;

        screenX = (((clip.X / clip.W) * 0.5) + 0.5) * viewWidth;
        screenY = (1.0 - (((clip.Y / clip.W) * 0.5) + 0.5)) * viewHeight;
        return true;
    }

    // A marker's swatch as a screen-aligned quad in clip space, where x points right and y up. The view size is in
    // pixels, like the swatch. False when the marker is behind the camera.
    public bool TryGetMarkerQuad(PanoramaMarker marker, double viewWidth, double viewHeight, out MarkerQuad quad)
    {
        quad = default;
        if (!TryNormalizedUvToScreen(marker.U, marker.V, viewWidth, viewHeight, out double x, out double y))
            return false;

        float centerX = (float)(((x + marker.OffsetX) / viewWidth * 2.0) - 1.0);
        float centerY = (float)(1.0 - ((y + marker.OffsetY) / viewHeight * 2.0));
        float halfWidth = (float)(marker.Width / viewWidth);
        float halfHeight = (float)(marker.Height / viewHeight);
        quad = new MarkerQuad(centerX - halfWidth, centerY + halfHeight, centerX + halfWidth, centerY - halfHeight);
        return true;
    }

    // The clip-space edges of a marker quad.
    public readonly record struct MarkerQuad(float Left, float Top, float Right, float Bottom);

    // Arguments for OrientedImageFootprint.UpdateFootprintAsync(yaw, pitch, hFov, vFov), all degrees: yaw clockwise
    // from the image's center column in [0, 360), pitch from the nadir (0) through the horizon (90) to the zenith (180).
    public readonly record struct FootprintView(double Yaw, double Pitch, double HorizontalFieldOfView, double VerticalFieldOfView);

    // Image column u = 0.5 + yaw/360 and row v = 1 - pitch/180, so yaw = 90 + Yaw and pitch = 90 - Pitch (degrees); the
    // horizontal field of view is the frustum's, widened from the vertical by the aspect ratio.
    public bool TryGetFootprintView(double viewWidth, double viewHeight, out FootprintView view)
    {
        view = default;
        if (viewWidth <= 0 || viewHeight <= 0)
            return false;

        if (!float.IsFinite(Yaw) || !float.IsFinite(Pitch) || !float.IsFinite(FieldOfView) || FieldOfView <= 0f || FieldOfView >= MathF.PI)
            return false;

        double yaw = (90.0 + (Yaw * 180.0 / Math.PI)) % 360.0;
        if (yaw < 0)
            yaw += 360.0;

        double pitch = Math.Clamp(90.0 - (Pitch * 180.0 / Math.PI), 0.0, 180.0);
        double verticalFov = FieldOfView * 180.0 / Math.PI;
        double horizontalFov = 2.0 * Math.Atan((viewWidth / viewHeight) * Math.Tan(FieldOfView / 2.0)) * 180.0 / Math.PI;
        view = new FootprintView(yaw, pitch, horizontalFov, verticalFov);
        return true;
    }

    // The renderers upload this matrix, so the drawn sphere and the click/marker math above cannot drift apart.
    internal Matrix4x4 GetWorldViewProjection(float aspectRatio)
    {
        Matrix4x4 world = Matrix4x4.CreateRotationY(Yaw) * Matrix4x4.CreateRotationX(Pitch);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, aspectRatio, NearPlane, FarPlane);
        return world * projection;
    }

    // The unit sphere every renderer draws, in the convention above: xyz positions, uv texture coordinates and a
    // 16-bit triangle list (signed for the GL buffers; the values fit either way).
    internal static (float[] Vertices, short[] Indices) CreateSphereMesh()
    {
        const int longitudeSegments = 64;
        const int latitudeSegments = 32;
        int vertexCount = (longitudeSegments + 1) * (latitudeSegments + 1);
        float[] vertices = new float[vertexCount * 5];
        int vertexOffset = 0;
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float v = lat / (float)latitudeSegments;
            float phi = v * MathF.PI;
            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float u = lon / (float)longitudeSegments;
                float theta = u * 2f * MathF.PI;
                vertices[vertexOffset++] = MathF.Sin(phi) * MathF.Cos(theta);
                vertices[vertexOffset++] = MathF.Cos(phi);
                vertices[vertexOffset++] = MathF.Sin(phi) * MathF.Sin(theta);
                vertices[vertexOffset++] = u;
                vertices[vertexOffset++] = v;
            }
        }

        short[] indices = new short[longitudeSegments * latitudeSegments * 6];
        int i = 0;
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                short first = (short)((lat * (longitudeSegments + 1)) + lon);
                short second = (short)(first + longitudeSegments + 1);
                indices[i++] = first;
                indices[i++] = second;
                indices[i++] = (short)(first + 1);
                indices[i++] = (short)(first + 1);
                indices[i++] = second;
                indices[i++] = (short)(second + 1);
            }
        }

        return (vertices, indices);
    }
}

// A marker as a panoramic surface draws it: a premultiplied BGRA8 swatch, the normalized (u,v) of its anchor, and the
// offset from the anchor to the swatch center, in pixels with y down. Bgra belongs to the panoramic viewport's swatch
// cache, so surfaces must not change it.
internal readonly record struct PanoramaMarker(float U, float V, byte[] Bgra, int Width, int Height, float OffsetX, float OffsetY)
{
    // An empty swatch, or one with fewer pixels than its size, can't be uploaded.
    public bool IsValid => Width > 0 && Height > 0 && Bgra.Length >= Width * Height * 4;
}
#endif
