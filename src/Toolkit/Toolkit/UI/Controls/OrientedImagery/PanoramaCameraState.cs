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

// Platform-neutral inverse of the panorama render projection: converts between a screen point and a normalized
// equirectangular texture coordinate (u,v) in [0,1]. Pair (u,v) with the source image pixel dimensions
// to reach the image-pixel space the SDK's OrientedImage.ImageToLocationAsync / contract clicks use.
//
// This type is the AUTHORITATIVE definition of the panorama coordinate conventions. Every platform renderer
// (D3D11 on Windows, GLES on Android, SceneKit on Apple) must generate its sphere mesh and camera matrices to
// match, or screen<->pixel math (clicks, markers, footprints) is mirrored or offset on that platform:
//   - Sphere: unit sphere around the camera, parameterized x = sin(phi)*cos(theta), y = cos(phi),
//     z = sin(phi)*sin(theta), with equirectangular UV u = theta/2pi (wrap), v = phi/pi (clamp).
//     v = 0 is up (phi = 0), v = 1 is down; the horizon is v = 0.5.
//   - Camera: right-handed, at the origin looking down -Z; world = RotationY(Yaw) * RotationX(Pitch)
//     (System.Numerics row-vector convention). Yaw/Pitch/FieldOfView are radians; FieldOfView is vertical.
//     At Yaw = 0, Pitch = 0 the view center is (u, v) = (0.75, 0.5); u_center = 0.75 + Yaw/2pi (mod 1)
//     and v_center = 0.5 + Pitch/pi (positive pitch looks below the horizon). The initial view heading is
//     set by the display as Yaw = -CameraHeading (see OrientedImagePanoramicDisplay).
//   - Screen: element/DIP coordinates, origin top-left, +Y down.
//   - Footprint: the SDK's 360 footprint update takes degrees with yaw 0 at the image's center column and pitch 0
//     at the nadir; TryGetFootprintView converts to it.
// GL note: System.Numerics matrices are row-major with row-vector math (clip = v * M). A GLSL mat4 uniform
// reads the same bytes column-major, i.e. as M-transposed, and computes M^T * v == v * M - so upload the raw
// floats with transpose = false; do not transpose them yourself.
internal readonly struct PanoramaCameraState
{
    private const float NearPlane = 0.1f;
    private const float FarPlane = 10f;

    // Shared camera limits and input tuning, used by every platform's camera/gesture layer.
    public const float MinPitch = -(MathF.PI / 2f) + 0.01f;
    public const float MaxPitch = (MathF.PI / 2f) - 0.01f;
    public const float MinFieldOfView = 50f * MathF.PI / 180f;
    public const float MaxFieldOfView = 120f * MathF.PI / 180f;
    public const float MouseRotationScale = 0.0035f; // fallback drag scale while the view size is unknown
    public const float KeyboardRotationDelta = MathF.PI / 90f;

    // The viewport spans fieldOfView (vertical, radians) across viewHeight DIPs.
    // Drag rotation in rad-per-DIP so the grabbed point tracks the pointer at screen center,
    // independent of control size, zoom and DPI. Same factor for yaw (the aspect's width cancels).
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

    public float Yaw { get; }

    public float Pitch { get; }

    public float FieldOfView { get; }

    // Screen point (element/DIP coordinates, origin top-left) -> normalized equirectangular (u,v) in [0,1].
    // Inverse of the render's world*projection; matches the sphere mesh parameterization (u = theta/2pi, v = phi/pi).
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

    // Normalized (u,v) -> screen point (element/DIP coordinates). Forward projection, for placing markers.
    // Returns false when the point is behind the camera (outside the current view).
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

    // The view in the oriented imagery 360 footprint convention, for
    // OrientedImageFootprint.UpdateFootprintAsync(yaw, pitch, horizontalFov, verticalFov). All values are degrees:
    // Yaw is clockwise from the image's center column in [0, 360); Pitch runs from the nadir (0) through the
    // horizon (90) to the zenith (180); the fields of view are the angles the viewport's width and height span.
    public readonly record struct FootprintView(double Yaw, double Pitch, double HorizontalFieldOfView, double VerticalFieldOfView);

    // Converts this camera to the footprint convention. The image maps its column u = 0.5 + yaw/360 and its row
    // v = 1 - pitch/180, so with the center anchors above (u_center = 0.75 + Yaw/2pi, v_center = 0.5 + Pitch/pi)
    // yaw = 90 deg + Yaw and pitch = 90 deg - Pitch. The horizontal field of view is the perspective frustum's,
    // widened from the vertical one by the aspect ratio. False when the viewport has no size or the camera is invalid.
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

    private Matrix4x4 GetWorldViewProjection(float aspectRatio)
    {
        Matrix4x4 world = Matrix4x4.CreateRotationY(Yaw) * Matrix4x4.CreateRotationX(Pitch);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, aspectRatio, NearPlane, FarPlane);
        return world * projection;
    }
}
#endif
