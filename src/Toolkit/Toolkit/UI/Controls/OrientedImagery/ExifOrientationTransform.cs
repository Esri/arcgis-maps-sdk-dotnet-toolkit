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

using System;
using System.Buffers.Binary;
using System.IO;

using PointF = System.Drawing.PointF;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// Maps stored JPEG pixels to the EXIF-oriented pixel space used by OrientedImage, and back.
// Missing metadata, including the default struct value, means identity.
// This is a minimal implementation inspired by https://stackoverflow.com/q/7584794/383361
// Full spec: https://www.cipa.jp/std/documents/download_e.html?CIPA_DC-008-2026-E
internal readonly struct ExifOrientationTransform
{
    private readonly int _value;

    internal ExifOrientationTransform(int value) => _value = value;

    internal bool SwapsDimensions => _value is >= 5 and <= 8;

    internal bool IsMirrored => _value is 2 or 4 or 5 or 7;

    // Clockwise rotation from the stored grid to the oriented image. A mirrored orientation then also reflects the
    // rotated image horizontally, so a view that can only rotate still shows it upright, though reversed.
    internal double RotationDegrees => _value switch
    {
        3 or 4 => 180,
        5 or 6 => 90,
        7 or 8 => 270,
        _ => 0,
    };

    // Both mappings take the stored image's width and height. They use the full size rather than the last pixel index
    // because footprint vertices can lie on the image boundary.
    internal PointF StoredToImage(PointF point, double width, double height) => _value switch
    {
        2 => new((float)(width - point.X), point.Y),
        3 => new((float)(width - point.X), (float)(height - point.Y)),
        4 => new(point.X, (float)(height - point.Y)),
        5 => new(point.Y, point.X),
        6 => new((float)(height - point.Y), point.X),
        7 => new((float)(height - point.Y), (float)(width - point.X)),
        8 => new(point.Y, (float)(width - point.X)),
        _ => point,
    };

    internal PointF ImageToStored(PointF point, double width, double height) => _value switch
    {
        2 => new((float)(width - point.X), point.Y),
        3 => new((float)(width - point.X), (float)(height - point.Y)),
        4 => new(point.X, (float)(height - point.Y)),
        5 => new(point.Y, point.X),
        6 => new(point.Y, (float)(height - point.X)),
        7 => new((float)(width - point.Y), (float)(height - point.X)),
        8 => new((float)(width - point.Y), point.X),
        _ => point,
    };

    internal static ExifOrientationTransform Read(Uri? dataUri) =>
        dataUri is { IsAbsoluteUri: true, IsFile: true } ? Read(dataUri.LocalPath) : default;

    internal static ExifOrientationTransform Read(string path)
    {
        try
        {
            // Open shared: the SDK owns the downloaded file.
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Read(stream);
        }
        catch (IOException)
        {
            return default;
        }
        catch (UnauthorizedAccessException)
        {
            return default;
        }
    }

    internal static ExifOrientationTransform Read(Stream stream)
    {
        // OrientedImage uses a TIFF's stored grid, so only a JPEG's orientation applies.
        if (stream.ReadByte() != 0xFF || stream.ReadByte() != 0xD8)
            return default;

        while (true)
        {
            int prefix = stream.ReadByte();
            if (prefix < 0)
                return default;
            if (prefix != 0xFF)
                continue;

            int marker;
            do
            {
                marker = stream.ReadByte();
            }
            while (marker == 0xFF);
            if (marker < 0 || marker == 0xDA || marker == 0xD9)
                return default; // EOF, start of scan, or end of image: no more metadata to inspect.

            if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                continue; // Standalone TEM/restart markers have no length field.

            int high = stream.ReadByte();
            int low = stream.ReadByte();
            if (high < 0 || low < 0)
                return default; // Truncated length field.

            // JPEG segment lengths are big-endian and include the two length bytes, but not the marker.
            int payloadLength = ((high << 8) | low) - 2;
            if (payloadLength < 0 || payloadLength > stream.Length - stream.Position)
                return default; // Malformed length field.

            // APP1 must fit the six-byte EXIF identifier and eight-byte TIFF header.
            if (marker == 0xE1 && payloadLength >= 14)
            {
                byte[] payload = new byte[payloadLength];
                stream.ReadExactly(payload);
                int orientation = ParseExifOrientation(payload);
                if (orientation is >= 1 and <= 8)
                    return new ExifOrientationTransform(orientation);
                // APP1 may contain XMP instead of EXIF; keep scanning if no Orientation tag was found.
            }
            else
            {
                stream.Seek(payloadLength, SeekOrigin.Current);
            }
        }
    }

    // Reads Orientation (1..8) from APP1: "Exif\0\0", a TIFF header, then IFD0. Zero means absent or invalid.
    private static int ParseExifOrientation(ReadOnlySpan<byte> app1)
    {
        if (!app1.StartsWith("Exif\0\0"u8))
            return 0;

        // EXIF offsets, including the one to IFD0, count from the start of the TIFF header.
        ReadOnlySpan<byte> tiff = app1[6..];
        if (tiff.Length < 8)
            return 0;

        bool littleEndian = tiff.StartsWith("II"u8);
        if (!littleEndian && !tiff.StartsWith("MM"u8))
            return 0; // Unknown byte order.

        if (ReadUInt16(tiff[2..], littleEndian) != 42)
            return 0; // Not a TIFF header.

        // Widened, so adding to a malformed offset can't wrap.
        long directory = ReadUInt32(tiff[4..], littleEndian);
        if (directory + 2 > tiff.Length)
            return 0;

        // IFD0 is a two-byte entry count, then 12-byte entries: tag, type, value count, and value.
        int count = ReadUInt16(tiff[(int)directory..], littleEndian);
        for (int index = 0; index < count; index++)
        {
            long start = directory + 2 + (index * 12);
            if (start + 12 > tiff.Length)
                return 0;

            // Orientation (0x0112) must be one SHORT (type 3), stored inline rather than at an offset.
            ReadOnlySpan<byte> entry = tiff.Slice((int)start, 12);
            if (ReadUInt16(entry, littleEndian) == 0x0112 &&
                ReadUInt16(entry[2..], littleEndian) == 3 &&
                ReadUInt32(entry[4..], littleEndian) == 1)
                return ReadUInt16(entry[8..], littleEndian);
        }

        return 0;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, bool littleEndian) =>
        littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(data) : BinaryPrimitives.ReadUInt16BigEndian(data);

    private static uint ReadUInt32(ReadOnlySpan<byte> data, bool littleEndian) =>
        littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(data) : BinaryPrimitives.ReadUInt32BigEndian(data);
}