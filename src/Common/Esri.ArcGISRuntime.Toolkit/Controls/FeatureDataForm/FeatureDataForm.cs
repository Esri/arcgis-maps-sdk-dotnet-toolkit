using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif
using Esri.ArcGISRuntime.Data;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// FeatureDataForm control.
    /// </summary>
    [TemplatePart(Name = "FeatureDataForm_ControlRoot", Type = typeof(Grid))]
    public class FeatureDataForm : Control
    {

        #region Private Members
        
        private Grid _controlRoot;                
        private GdbFeature _editFeature;
        private IEnumerable<string> _validFieldNames;
        private readonly IDictionary<string, FrameworkElement> _fieldControls = new Dictionary<string, FrameworkElement>();
        private readonly List<FieldType> _notSupportFieldTypes = new List<FieldType> // these types will not be supported for editing or display.
        {
            FieldType.Blob, FieldType.Geometry, 
            FieldType.Raster, FieldType.Xml, FieldType.Unknown
        };

        #endregion Private Members

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureDataForm"/> class.
        /// </summary>
        public FeatureDataForm()
        {
#if NETFX_CORE || WINDOWS_PHONE
            DefaultStyleKey = typeof(FeatureDataForm);
#endif
            Fields = new ObservableCollection<string>();
            ApplyCommand = new ActionCommand(ApplyChanges,CanApplyChanges);
            ResetCommand = new ActionCommand(Cancel,CanCancel);
        }
#if !NETFX_CORE && !WINDOWS_PHONE
        static FeatureDataForm()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (FeatureDataForm),
                new FrameworkPropertyMetadata(typeof (FeatureDataForm)));
        }
#endif

        #endregion

        #region Overrides

#if NETFX_CORE
        /// <summary>
        /// Invoked whenever application code or internal 
        /// processes (such as a rebuilding layout pass) 
        /// call ApplyTemplate. In simplest terms, this 
        /// means the method is called just before a UI 
        /// element displays in your app. Override this 
        /// method to influence the default post-template 
        /// logic of a class.
        /// </summary>
        protected 
#else
        /// <summary>
        /// When overridden in a derived class, is invoked 
        /// whenever application code or internal processes 
        /// call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public
#endif
        override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _controlRoot =  (Grid)GetTemplateChild("FeatureDataForm_ControlRoot");

            Refresh();
        }

        #endregion Overrides

        #region Public Properties

        #region GdbFeature

        /// <summary>
        /// Gets or sets the GdbFeature that contains the 
        /// information needed to create fields and binding.
        /// </summary>        
        public GdbFeature GdbFeature
        {
            get { return (GdbFeature)GetValue(GdbFeatureProperty); }
            set { SetValue(GdbFeatureProperty, value); }
        }

        /// <summary>
        /// The dependency property for GdbFeature.
        /// </summary>
        public static readonly DependencyProperty GdbFeatureProperty =
            DependencyProperty.Register("GdbFeature", typeof(GdbFeature), typeof(FeatureDataForm), new PropertyMetadata(null, OnGdbFeaturePropertyChanged));

        private static void OnGdbFeaturePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataForm = ((FeatureDataForm) d);
            dataForm._editFeature = ((GdbFeature) e.NewValue).Clone();
            dataForm.Refresh();
        }

        #endregion GdbFeature

        #region Fields

        /// <summary>
        /// Gets or sets the fields to display. A field will be 
        /// displayed according to the index of this property.
        /// </summary>        
        /// <remarks>
        /// <list type="bullet">
        /// <item>if Fields property is "null" or "empty" all supported editable fields will be displayed.</item>
        /// <item>if Fields property contains "*" then all supported fields will be displayed editable and non-editable.</item>
        /// <item>if Fields property contains a specific set of fields "&lt;FieldName1&gt;,&lt;FieldName2&gt;" then only these 
        /// fields show in the order they are provided. Field names are case sensitive and only supported 
        /// field types will be rendered.</item>
        /// </list>               
        /// <pre>
        /// The non-supported field types are XML, Blob, Geometry and Raster.
        /// </pre>
        /// </remarks>
        public ObservableCollection<string> Fields
        {
            get { return (ObservableCollection<string>)GetValue(FieldsProperty); }
            set { SetValue(FieldsProperty, value); }
        }

        /// <summary>
        /// The dependency property for the Fields property.
        /// </summary>
        public static readonly DependencyProperty FieldsProperty =
            DependencyProperty.Register("Fields", typeof(ObservableCollection<string>), typeof(FeatureDataForm), new PropertyMetadata(null, OnFieldsPropertyChanged));

        private static void OnFieldsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataForm)d).Refresh();
        }

        #endregion Fields

        #region LabelStyle

        /// <summary>
        /// Gets or sets the label style. This property can be used 
        /// to set the label style for all fields in the FeatureDataForm.
        /// </summary>      
        public Style LabelStyle
        {
            get { return (Style)GetValue(LabelStyleProperty); }
            set { SetValue(LabelStyleProperty, value); }
        }
        
        /// <summary>
        /// The dependency property for the LabelStyle property.
        /// </summary>
        public static readonly DependencyProperty LabelStyleProperty =
            DependencyProperty.Register("LabelStyle", typeof(Style), typeof(FeatureDataForm), new PropertyMetadata(null, OnLabelStylePropertyChanged));

        private static void OnLabelStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataForm)d).Refresh();
        }

        #endregion LabelStyle

        #region ContainerStyle

        /// <summary>
        /// Gets or sets the container style. This property can be used 
        /// to set the container style for all fields in the FeatureDataForm.
        /// </summary>
        public Style ContainerStyle
        {
            get { return (Style)GetValue(ContainerStyleProperty); }
            set { SetValue(ContainerStyleProperty, value); }
        }

        /// <summary>
        /// The dependency property for the ContainerStyle property.
        /// </summary>
        public static readonly DependencyProperty ContainerStyleProperty =
            DependencyProperty.Register("ContainerStyle", typeof(Style), typeof(FeatureDataForm), new PropertyMetadata(null, OnContainerStylePropertyChanged));

        private static void OnContainerStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataForm)d).Refresh();
        }

        #endregion ContainerStyle

        #region InputTemplate

        /// <summary>
        /// Gets or sets the input template. This property can be used 
        /// to set the input template for all fields in the FeatureDataForm.
        /// </summary>        
        public DataTemplate InputTemplate
        {
            get { return (DataTemplate)GetValue(InputTemplateProperty); }
            set { SetValue(InputTemplateProperty, value); }
        }
        
        /// <summary>
        /// The dependency property for the InputTemplate property.
        /// </summary>
        public static readonly DependencyProperty InputTemplateProperty =
            DependencyProperty.Register("InputTemplate", typeof(DataTemplate), typeof(FeatureDataForm), new PropertyMetadata(null, OnInputTemplatePropertyChanged));

        private static void OnInputTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataForm)d).Refresh();
        }

        #endregion InputTemplate        

        #region DateTimeTemplate

        /// <summary>
        /// Gets or sets the date time template. This property can be used 
        /// to set the date time template for all fields in the FeatureDataForm.
        /// </summary>  
        public DataTemplate DateTimeTemplate
        {
            get { return (DataTemplate)GetValue(DateTimeTemplateProperty); }
            set { SetValue(DateTimeTemplateProperty, value); }
        }

        /// <summary>
        /// The dependency property for the DateTimeTemplate property.
        /// </summary>
        public static readonly DependencyProperty DateTimeTemplateProperty =
            DependencyProperty.Register("DateTimeTemplate", typeof(DataTemplate), typeof(FeatureDataForm), new PropertyMetadata(null, OnDateTimeTemplatePropertyChanged));

        private static void OnDateTimeTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataForm)d).Refresh();
        }

        #endregion DateTimeTemplate

        #region IsReadOnly

        /// <summary>
        /// Gets or sets a value indicating whether the form is read only.
        /// </summary>        
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }
        
        /// <summary>
        /// The dependency property for the IsReadOnly property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(FeatureDataForm), new PropertyMetadata(false, OnIsReadOnlyPropertyChanged));

        private static void OnIsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataForm)d).Refresh();
        }

        #endregion IsReadOnly

        #region ApplyCommand

        /// <summary>
        /// Gets the apply command which an be used to apply 
        /// any edits that have been made to the FeatureDataForm.
        /// </summary>        
        public ICommand ApplyCommand
        {
            get { return (ICommand)GetValue(ApplyCommandProperty); }
            private set { SetValue(ApplyCommandProperty, value); }
        }

        /// <summary>
        /// The dependency property for the <see cref="FeatureDataForm.ApplyCommand"/>.
        /// </summary>
        public static readonly DependencyProperty ApplyCommandProperty =
            DependencyProperty.Register("ApplyCommand", typeof(ICommand), typeof(FeatureDataForm), new PropertyMetadata(null));

        #endregion ApplyCommand

        #region ResetCommand

        /// <summary>
        /// Gets the cancel command which an be used to cancel 
        /// any edits that have been made to the FeatureDataForm.
        /// </summary>        
        public ICommand ResetCommand
        {
            get { return (ICommand)GetValue(ResetCommandProperty); }
            private set { SetValue(ResetCommandProperty, value); }
        }


        /// <summary>
        /// The dependency property for the <see cref="ResetCommand"/>
        /// </summary>
        public static readonly DependencyProperty ResetCommandProperty =
            DependencyProperty.Register("ResetCommand", typeof(ICommand), typeof(FeatureDataForm), new PropertyMetadata(null));

        #endregion ResetCommand

        #region HasEdits

        /// <summary>
        /// Gets a value indicating whether the form has
        /// edits that haven't been saved.
        /// </summary>        
        public bool HasEdits
        { 
            get { return (bool)GetValue(HasEditsProperty); }
            private set { SetValue(HasEditsProperty, value); } 
        }
  
        /// <summary>
        /// The dependency property for <see cref="FeatureDataForm.HasEdits"/>.
        /// </summary>
         public static readonly DependencyProperty HasEditsProperty = 
             DependencyProperty.Register("HasEdits", typeof(bool), typeof(FeatureDataForm), new PropertyMetadata(false));
 

        
        #endregion HasEdits

        #region HasError

        /// <summary>
        /// Get that value indicating whether the form
        /// has any fields that have a validation excption.
        /// </summary>
        public bool HasError
        {
            get { return (bool)GetValue(HasErrorProperty); }
            private set { SetValue(HasErrorProperty, value); }
        }

        /// <summary>
        /// The dependency property for <see cref="FeatureDataForm.HasError"/>.
        /// </summary>
        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register("HasError", typeof(bool), typeof(FeatureDataForm), new PropertyMetadata(false));
        

        #endregion HasError       

        #endregion Public Properties

        #region Public Events

        /// <summary>
        /// Occurs when each fields UI is being generated.
        /// </summary>
        public event EventHandler<GeneratingFieldEventArgs> GeneratingField;               

        /// <summary>
        /// Occurs when changes are applied to the GdbFeature.
        /// </summary>
        public event EventHandler<EventArgs> ApplyCompleted;
        
        #endregion Public Events

        #region Private Methods

        /// <summary>
        /// Creates the UI of the entire control
        /// </summary>
        private void Refresh()
        {
            if (_controlRoot != null && _editFeature != null && GdbFeature.Schema != null && Fields != null)
            {
                // Clear root element children.
                _controlRoot.Children.Clear();
                _controlRoot.ColumnDefinitions.Clear();
                foreach (var control in _fieldControls.Keys.Select(key => _fieldControls[key]).OfType<FeatureDataField>())
                {                    
                    control.PropertyChanged -= ControlOnPropertyChanged;
                }                
                _fieldControls.Clear();                
                HasEdits = false;
                HasError = false;

                // Get collection of fields with supported field types.
                var supportedFields = _editFeature.Schema.Fields.Where(fieldInfo => !_notSupportFieldTypes.Contains(fieldInfo.Type));

                if (Fields == null || !Fields.Any())
                {
                    // default to only editable fields
                    var editableSupportedFields = supportedFields.Where(fieldInfo => fieldInfo.IsEditable);
                    _validFieldNames = editableSupportedFields.Select(fieldInfo => fieldInfo.Name);
                }
                else
                {
                    _validFieldNames = Fields.Contains("*") 
                        ? supportedFields.Select(fieldInfo => fieldInfo.Name) // All Fields (*)
                        : Fields.Intersect(from fieldInfo in supportedFields select fieldInfo.Name); // specific fields provided by user
                }                

                // Create UI for each field
                foreach (var fieldName in _validFieldNames)
                {
                    // Get the field information from schema for this field.
                    var fieldInfo = _editFeature.GetFieldInfo(fieldName);                                        

                    // default the label text to field alias if not null or empty
                    var labelText = !string.IsNullOrEmpty(fieldInfo.Alias) 
                        ? fieldInfo.Alias 
                        : fieldInfo.Name; 

                    var isReadOnly = false;          // default readonly to false
                    Style labelStyle = null;                    
                    Style containerStyle = null;
                    DataTemplate inputTemplate = null;
                    DataTemplate dateTimeTemplate = null;


                    // if user has wired into the on generating event we will expose 
                    // some overridable information for each field.
                    if (GeneratingField != null)
                    {
                        // Create a new event argument for field.
                        var args = new GeneratingFieldEventArgs(fieldName, labelText, fieldInfo.Type) {IsReadOnly = IsReadOnly};

                        // Raise the event for user
                        GeneratingField(this, args);

                        // If user changed the label text or set the 
                        // field to read-only then we will use these 
                        // during UI creation.
                        labelText = args.LabelText;
                        isReadOnly = args.IsReadOnly;
                        labelStyle = args.LabelStyle;
                        inputTemplate = args.InputTemplate;
                        dateTimeTemplate = args.DateTimeTemplate;
                        containerStyle = args.ContainerStyle;
                    }

                    // create label ui
                    var label = CreateLabel(labelText);
                    label.Style = labelStyle ?? LabelStyle;
                    
                    // create edit control ui
                    var control = CreateControl(_editFeature, fieldInfo, IsReadOnly ? IsReadOnly : isReadOnly);
                   
                    if (fieldInfo.Type == FieldType.Date)
                    {                       
                        // Form or Field override of input template property.
                        if(dateTimeTemplate != null || DateTimeTemplate != null)
                            ((FeatureDataField)control).InputTemplate = dateTimeTemplate ?? DateTimeTemplate;                    
                        else if (inputTemplate != null || InputTemplate != null)
                            ((FeatureDataField)control).InputTemplate = inputTemplate ?? InputTemplate;                    
                    }
                    else
                    {
                        // Form or Field override of input template property.
                        if (inputTemplate != null || InputTemplate != null)
                            ((FeatureDataField)control).InputTemplate = inputTemplate ?? InputTemplate;                    
                    }                   
                    
                    // create container control
                    var container = CreateContainer();
                    container.Style = containerStyle ?? ContainerStyle;

                    // Add label and edit control to container
                    container.Children.Add(label);
                    container.Children.Add(control);

                    // Set the Grid.Row attached property to the container
                    Grid.SetRow(container,_controlRoot.RowDefinitions.Count);

                    // Create a new RowDefinition for the container
                    _controlRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Add each edit control to a lookup table that can be used 
                    // to invoke save and cancel commands on FeatureDataField.
                    _fieldControls.Add(fieldName, control);

                    // Add continer to the Grid
                    _controlRoot.Children.Add(container);
                }
            }
        }

        /// <summary>
        /// Creates a UI container for each field.
        /// </summary>        
        private Panel CreateContainer()
        {
            return new StackPanel();
        }

        /// <summary>
        /// Creates a UI label for each field
        /// </summary>
        /// <param name="labelText">The label text.</param>        
        private static FrameworkElement CreateLabel(string labelText)
        {
            var label = new TextBlock
            {
                Text = labelText,               
            };            
            return label;
        }

        /// <summary>
        /// Creates a UI control for editing each field.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="fieldInfo">FieldInfo for edit control</param>
        /// <param name="isReadOnly">Value indicating if control should be readonly</param>
        /// <returns></returns>
        private FrameworkElement CreateControl(GdbFeature feature, FieldInfo fieldInfo, bool isReadOnly)
        {
            var control = new FeatureDataField
            {
                GdbFeature = feature,
                FieldName = fieldInfo.Name,
                IsReadOnly = isReadOnly,                
            };                        
            control.PropertyChanged += ControlOnPropertyChanged;
            return control;
        }        

        /// <summary>
        /// Watch FeatureDataField for changes to Value and ValidationException in order to know when 
        /// SaveCommand and ResetCommand can be active.
        /// </summary>
        /// <param name="sender">FeatureDataField</param>
        /// <param name="propertyChangedEventArgs">Information about which property changed.</param>
        private void ControlOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            switch (propertyChangedEventArgs.PropertyName)
            {
                case "BindingValue":                    
                    HasEdits = HasChanges();
                    HasError = CheckForError();
                    ((ActionCommand)ApplyCommand).RaiseCanExecute();
                    ((ActionCommand)ResetCommand).RaiseCanExecute();
                    break;                                
            }
        }

        private bool CheckForError()
        {
            if (_fieldControls == null) return false;
            return _fieldControls.Keys.Select(fieldName => (FeatureDataField) _fieldControls[fieldName]).Any(control => control.ValidationException != null);
        }


        /// <summary>
        /// This will save edit featue changes back to original feature.
        /// </summary>
        private void ApplyChanges()
        {
            if (_editFeature == null || GdbFeature == null) return;
            
            // copy edit feature back to original feature
            GdbFeature.CopyFrom(_editFeature.Attributes);
            HasEdits = false;
            HasError = false;
            ((ActionCommand)ApplyCommand).RaiseCanExecute();
            ((ActionCommand)ResetCommand).RaiseCanExecute();

            // Notify user that Apply has been completed.
            var handler = ApplyCompleted;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// This will return true if the FeatureDataForm has 
        /// changes and none of the changes are invalid.
        /// </summary>        
        private bool CanApplyChanges()
        {
            return HasEdits && !HasError;
        }

        /// <summary>
        /// This will override all edit feature changes back
        /// to the original feature values.
        /// </summary>
        private void Cancel()
        {
            if (_editFeature == null || GdbFeature == null) return;
            
            // copy original feature back to edit feature
            _editFeature.CopyFrom(GdbFeature.Attributes);
            HasEdits = false;
            HasError = false;                 
            ((ActionCommand)ApplyCommand).RaiseCanExecute();
            ((ActionCommand)ResetCommand).RaiseCanExecute();
            Refresh(); 
        }

        /// <summary>
        /// This will return true if a change has 
        /// been made to the edit feature.
        /// </summary>        
        private bool CanCancel()
        {
            return HasEdits;
        }

        /// <summary>
        /// Compares orignial GdbFeature to the clone GdbFeature 
        /// to check for differences in attribute values.
        /// </summary>        
        private bool HasChanges()
        {            
            if (_editFeature != null && GdbFeature != null)            
                return _validFieldNames.Any(fieldName => !AreEqual(
                    (_editFeature.Attributes.ContainsKey(fieldName) ? _editFeature.Attributes[fieldName] : null), 
                    (GdbFeature.Attributes.ContainsKey(fieldName) ? GdbFeature.Attributes[fieldName] : null)));            
            return false;
        }

        // Simplified equality test that handles 
        // nulls, values, and reference
        private static bool AreEqual(object o1, object o2)
        {
            return (o1 == o2) || (o1 != null && o1.Equals(o2));
        }        

        #endregion Private Methods              
    }
}