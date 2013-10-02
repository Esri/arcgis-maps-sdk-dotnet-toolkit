namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// Base class for ValueChangeing and ValueChange events for FeatureDataField.
    /// </summary>
    public abstract class ValueEventArgs
    {
        internal ValueEventArgs(object oldValue, object newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the old value.
        /// </summary>
        public object OldValue { get; private set; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public object NewValue { get; private set; }
    }
}