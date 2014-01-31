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
