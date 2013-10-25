using System;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// Event args used for the ValueChanging event of FeatureDataField.
    /// </summary>
    public sealed class ValueChangingEventArgs : ValueEventArgs
    {
        internal ValueChangingEventArgs(object oldValue, object newValue) 
            : base(oldValue, newValue)
        {}

        /// <summary>
        /// Sets the validation exception. Used to trigger 
        /// validation state for user defined validation.
        /// </summary>     
        public Exception ValidationException { internal get; set; }
    }
}
