#if ANDROID
using Android.Graphics;
using RectF = Android.Graphics.RectF;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    internal static class TransformUtils
    {
        /// <summary>
        /// Rotates the given <paramref name="rect"/> by the given number of degrees <paramref name="rotationDegrees"/>.
        /// </summary>
        public static RectF RotateRect(RectF rect, int rotationDegrees)
        {
            int clampedRotation = (rotationDegrees % 360 + 360) % 360;
            return clampedRotation == 90 || clampedRotation == 270
                ? new RectF(0, 0, rect.Height(), rect.Width())
                : rect;
        }

        /// <summary>
        /// Returns a transformation matrix that maps a source rectangle to a target rectangle
        /// with the given rotation <paramref name="rotationDegrees"/>.
        /// </summary>
        public static Matrix GetRectToRect(RectF source, RectF target, int rotationDegrees)
        {
            var matrix = new Matrix();
            // Map source to normalized space.
            matrix.SetRectToRect(source, new RectF(-1, -1, 1, 1), Matrix.ScaleToFit.Fill);
            // Add rotation.
            matrix.PostRotate(rotationDegrees);
            // Restore the normalized space to target's coordinates.
            matrix.PostConcat(GetNormalizedToBuffer(target));
            return matrix;
        }

        /// <summary>
        /// Returns a transformation matrix that maps a normalized rectangle to a target rectangle.
        /// </summary>
        private static Matrix GetNormalizedToBuffer(RectF viewPortRect)
        {
            var normalizedToBuffer = new Matrix();
            normalizedToBuffer.SetRectToRect(
                new RectF(-1, -1, 1, 1),
                viewPortRect,
                Matrix.ScaleToFit.Fill
            );
            return normalizedToBuffer;
        }
    }
}
#endif