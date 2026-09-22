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

    public float Yaw { get; }

    public float Pitch { get; }

    public float FieldOfView { get; }

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
    internal static (float[] Positions, float[] TexCoords, short[] Indices) CreateSphereMesh()
    {
        const int longitudeSegments = 64;
        const int latitudeSegments = 32;
        int vertexCount = (longitudeSegments + 1) * (latitudeSegments + 1);
        float[] positions = new float[vertexCount * 3];
        float[] texCoords = new float[vertexCount * 2];
        int p = 0, t = 0;
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float v = lat / (float)latitudeSegments;
            float phi = v * MathF.PI;
            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float u = lon / (float)longitudeSegments;
                float theta = u * 2f * MathF.PI;
                positions[p++] = MathF.Sin(phi) * MathF.Cos(theta);
                positions[p++] = MathF.Cos(phi);
                positions[p++] = MathF.Sin(phi) * MathF.Sin(theta);
                texCoords[t++] = u;
                texCoords[t++] = v;
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

        return (positions, texCoords, indices);
    }
}
#endif
