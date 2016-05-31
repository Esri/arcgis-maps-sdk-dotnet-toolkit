// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://opensource.org/licenses/ms-pl for details.
// All other rights reserved.

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// Event args used for the ValueChanged event of FeatureDataField.
    /// </summary>
    public sealed class ValueChangedEventArgs : ValueEventArgs
    {
        internal ValueChangedEventArgs(object oldValue, object newValue) : base(oldValue, newValue)
        {
        }
    }
}
