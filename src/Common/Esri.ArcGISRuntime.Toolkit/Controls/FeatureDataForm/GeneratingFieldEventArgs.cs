using System;
using Esri.ArcGISRuntime.Data;
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
        internal GeneratingFieldEventArgs(string fieldName, string labelText, FieldType fieldType)
        {
            FieldName = fieldName;
            LabelText = labelText;
            FieldType = fieldType;
        }

        /// <summary>
        /// Gets the name of the field being created.
        /// </summary>
        public string FieldName { get; private set; }


        /// <summary>
        /// Gets the field type being created.
        /// </summary>        
        public FieldType FieldType { get; private set; }

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
        /// Gets or sets the fields InputTemplate property.
        /// </summary>        
        public DataTemplate InputTemplate { get; set; }

        /// <summary>
        /// Gets or sets the field DateTimeTemplate property
        /// </summary>
        public DataTemplate DateTimeTemplate { get; set; }
    }
}