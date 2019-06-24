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
using CoreGraphics;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Helper class for providing common cross-platform names for members having to do with graphics or drawing-related objects
    /// </summary>
    internal static class GraphicsExtensions
    {
        public static bool IsEmpty(this CGSize size) => size.IsEmpty;

        public static void SetX(ref this CGRect rect, double x) => rect.X = (nfloat)x;

        public static void SetWidth(ref this CGRect rect, double width) => rect.Width = (nfloat)width;
    }
}
