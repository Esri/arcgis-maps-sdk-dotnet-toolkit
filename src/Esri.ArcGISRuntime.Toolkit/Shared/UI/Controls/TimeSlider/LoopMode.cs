using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Specifies the loop behavior of temporal playback
    /// </summary>
    public enum LoopMode
    {
        /// <summary>
        /// Specifies that playback should not loop when the bounds of the temporal extent are reached
        /// </summary>
        None,
        /// <summary>
        /// Specifies that temporal playback should repeat when the bounds of the temporal extent are reached
        /// </summary>
        Repeat,
        /// <summary>
        /// Specifies that temporal playback should reverse direction when the bounds of the temporal extent are reached
        /// </summary>
        Reverse
    }
}
