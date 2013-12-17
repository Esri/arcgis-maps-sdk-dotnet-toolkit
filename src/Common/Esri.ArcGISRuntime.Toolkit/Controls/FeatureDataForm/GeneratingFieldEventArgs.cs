using System;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// GeneratingFieldEventArgs contains information about a 
    /// field that will define how the field will be rendered. 
    /// Properties can be changed to customize what is rendered.
    /// </summary>
    public sealed class GeneratingFieldEventArgs : EventArgs
    {
        internal GeneratingFieldEventArgs(string fieldName, string labelText)
        {
            FieldName = fieldName;
            LabelText = labelText;
        }

        /// <summary>
        /// Gets the name of the field being created.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets or sets the label text.
        /// </summary>        
        public string LabelText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// this field is readonly.
        /// </summary>        
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the label style.
        /// </summary>        
        public Style LabelStyle { get; set; }

        /// <summary>
        /// Gets or sets the container style.
        /// </summary>
        public Style ContainerStyle { get; set; }

        /// <summary>
        /// Gets or sets the item style.
        /// </summary>        
        public Style ItemStyle { get; set; }
    }
}