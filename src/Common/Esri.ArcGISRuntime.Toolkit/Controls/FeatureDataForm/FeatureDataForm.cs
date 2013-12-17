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
        private IDictionary<string, FrameworkElement> _fieldControls = new Dictionary<string, FrameworkElement>();
        private List<FieldType> _notSupportFieldTypes = new List<FieldType> // these types will not be supported for editing or display.
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
            CancelCommand = new ActionCommand(Cancel,CanCancel);
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

        #region ItemStyle

        /// <summary>
        /// Gets or sets the item style. This property can be used 
        /// to set the item style for all fields in the FeatureDataForm.
        /// </summary>        
        public Style ItemStyle
        {
            get { return (Style)GetValue(ItemStyleProperty); }
            set { SetValue(ItemStyleProperty, value); }
        }
        
        /// <summary>
        /// The dependency property for the ItemStyle property.
        /// </summary>
        public static readonly DependencyProperty ItemStyleProperty =
            DependencyProperty.Register("ItemStyle", typeof(Style), typeof(FeatureDataForm), new PropertyMetadata(null, OnItemStylePropertyChanged));

        private static void OnItemStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataForm)d).Refresh();
        }

        #endregion ItemStyle

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

        #region CancelCommand

        /// <summary>
        /// Gets the cancel command which an be used to cancel 
        /// any edits that have been made to the FeatureDataForm.
        /// </summary>        
        public ICommand CancelCommand
        {
            get { return (ICommand)GetValue(CancelCommandProperty); }
            private set { SetValue(CancelCommandProperty, value); }
        }


        /// <summary>
        /// The dependency property for the <see cref="FeatureDataForm.CancelCommand"/>
        /// </summary>
        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(FeatureDataForm), new PropertyMetadata(null));

        #endregion CancelCommand

        #region HasEdit

        /// <summary>
        /// Gets a value indicating whether the form has
        /// edits that haven't been saved.
        /// </summary>        
        public bool HasEdit
        {
            get { return (bool)GetValue(HasEditProperty); }
            private set { SetValue(HasEditProperty, value); }
        }

        /// <summary>
        /// The dependency property for <see cref="FeatureDataForm.HasEdit"/>.
        /// </summary>
        public static readonly DependencyProperty HasEditProperty =
            DependencyProperty.Register("HasEdit", typeof(bool), typeof(FeatureDataForm), new PropertyMetadata(false));

        #endregion HasEdit

        #region HasError

        /// <summary>
        /// Get that value indicating whether the form
        /// has any field that has a validation excption.
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
        //public event EventHandler EditCompleted;

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
                HasEdit = false;
                HasError = false;

                // Get collection of fields with supported field types.
                var supportedFields = _editFeature.Schema.Fields.Where(fieldInfo => !_notSupportFieldTypes.Contains(fieldInfo.Type));

                // Only use field names provided by user if they exist as part of the Schema
                _validFieldNames = Fields.Intersect(from fieldInfo in supportedFields select fieldInfo.Name);

                // Create UI for each field
                foreach (var fieldName in _validFieldNames)
                {
                    // Get the field information from schema for this field.
                    var fieldInfo = _editFeature.GetFieldInfo(fieldName);

                    var labelText = fieldInfo.Alias; // default the label text to field alias
                    var isReadOnly = false;          // default readonly to false
                    Style labelStyle = null;
                    Style itemStyle = null;
                    Style containerStyle = null;


                    // if user has wired into the on generating event we will expose 
                    // some overridable information for each field.
                    if (GeneratingField != null)
                    {
                        // Create a new event argument for field.
                        var args = new GeneratingFieldEventArgs(fieldName, labelText) {IsReadOnly = IsReadOnly};

                        // Raise the event for user
                        GeneratingField(this, args);

                        // If user changed the label text or set the 
                        // field to read-only then we will use these 
                        // during UI creation.
                        labelText = args.LabelText;
                        isReadOnly = args.IsReadOnly;
                        labelStyle = args.LabelStyle;
                        itemStyle = args.ItemStyle;
                        containerStyle = args.ContainerStyle;
                    }

                    // create label ui
                    var label = CreateLabel(labelText);
                    label.Style = labelStyle ?? LabelStyle;
                    
                    // create edit control ui
                    var control = CreatControl(_editFeature, fieldInfo, IsReadOnly ? IsReadOnly : isReadOnly);
                    control.Style = itemStyle ?? ItemStyle;

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
        private FrameworkElement CreatControl(GdbFeature feature, FieldInfo fieldInfo, bool isReadOnly)
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
        /// SaveCommand and CancelCommand can be active.
        /// </summary>
        /// <param name="sender">FeatureDataField</param>
        /// <param name="propertyChangedEventArgs">Information about which property changed.</param>
        private void ControlOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            switch (propertyChangedEventArgs.PropertyName)
            {
                case "BindingValue":                    
                    HasEdit = HasChange();
                    HasError = CheckForError();
                    ((ActionCommand)ApplyCommand).RaiseCanExecute();
                    ((ActionCommand)CancelCommand).RaiseCanExecute();
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
            HasEdit = false;
            HasError = false;
            ((ActionCommand)ApplyCommand).RaiseCanExecute();
            ((ActionCommand)CancelCommand).RaiseCanExecute();            
        }

        /// <summary>
        /// This will return true if the FeatureDataForm has 
        /// changes and none of the changes are invalid.
        /// </summary>        
        private bool CanApplyChanges()
        {
            return HasEdit && !HasError;
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
            HasEdit = false;
            HasError = false;                 
            ((ActionCommand)ApplyCommand).RaiseCanExecute();
            ((ActionCommand)CancelCommand).RaiseCanExecute();
            Refresh(); 
        }

        /// <summary>
        /// This will return true if a change has 
        /// been made to the edit feature.
        /// </summary>        
        private bool CanCancel()
        {
            return HasEdit;
        }

        /// <summary>
        /// Compares orignial GdbFeature to the clone GdbFeature 
        /// to check for differnces in attribute values.
        /// </summary>        
        private bool HasChange()
        {            
            if (_editFeature != null && GdbFeature != null)
            {
                foreach (var fieldName in _validFieldNames)
                {
                    if(!AreEqual(_editFeature.Attributes[fieldName], GdbFeature.Attributes[fieldName]))
                        return true;
                }
            }
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