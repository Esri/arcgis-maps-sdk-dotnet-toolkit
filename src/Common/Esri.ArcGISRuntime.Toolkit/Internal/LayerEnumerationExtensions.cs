// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Esri.ArcGISRuntime.Layers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class LayerEnumerationExtensions
    {
        // Enumerates leaves by taking care of a predicate at all levels of the hierarchy.
        // Useful for getting all visible layers by taking care of group layers visibility
        public static IEnumerable<Layer> EnumerateLeaves(this IEnumerable<Layer> layers, Func<Layer, bool> predicate)
        {
            if (layers != null)
            {
                foreach (Layer layer in layers.Where(predicate))
                {
                    var gl = layer as GroupLayer;
                    if (gl != null)
                    {
                        foreach (Layer child in EnumerateLeaves(gl.ChildLayers, predicate))
                            yield return child;
                    }
                    else
                        yield return layer;
                }
            }
        }
    }
}