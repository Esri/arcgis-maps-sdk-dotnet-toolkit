// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://opensource.org/licenses/ms-pl for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
#if NETFX_CORE          // Windows Store & Windows Phone
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
#else                   // WPF
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
	/// FeatureDatafield is used to edit or display a single attribute from a GeodatabaseFeature.
    /// </summary>
    [TemplatePart(Name = "FeatureDataField_ContentControl", Type = typeof(ContentControl))]
    public class FeatureDataField : Control, INotifyPropertyChanged
    {       
        #region Private Properties

        private ContentControl _contentControl;               
        private FieldInfo _fieldInfo;
        private DataItem _dataItem;
        private bool _focused;
        private string _currentState;
    
        #endregion Private Properties

        #region Constructor


        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureDataField"/> class.
        /// </summary>
        public FeatureDataField()
        {
#if NETFX_CORE
            DefaultStyleKey = typeof(FeatureDataField);
#endif            
        }

#if !NETFX_CORE
        static FeatureDataField()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FeatureDataField), new FrameworkPropertyMetadata(typeof(FeatureDataField)));
        }
#endif
        #endregion Constructor

        #region Public Fields

		#region GeodatabaseFeature

		/// <summary>
		/// Gets or sets the <see cref="Esri.ArcGISRuntime.Data.GeodatabaseFeature"/>.
        /// </summary>        
		public GeodatabaseFeature GeodatabaseFeature
        {
			get { return (GeodatabaseFeature)GetValue(GeodatabaseFeatureProperty); }
            set { SetValue(GeodatabaseFeatureProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="GeodatabaseFeature"/>.
        /// </summary>
        public static readonly DependencyProperty GeodatabaseFeatureProperty =
			DependencyProperty.Register("GeodatabaseFeature", typeof(GeodatabaseFeature), typeof(FeatureDataField), new PropertyMetadata(null, OnGeodatabaseFeaturePropertyChanged));

        private static void OnGeodatabaseFeaturePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = ((FeatureDataField)d);
            form.Refresh();
			form.OnPropertyChanged("GeodatabaseFeature");          
        }

		#endregion GeodatabaseFeature

		#region FieldName

		/// <summary>
		/// Gets or sets the name of the field from the GeodatabaseFeature.Attributes. 
        /// The UI input generated will be for this field.
        /// </summary>       
        public string FieldName
        {
            get { return (string)GetValue(FieldNameProperty); }
            set { SetValue(FieldNameProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for FieldName.
        /// </summary>
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register("FieldName", typeof(string), typeof(FeatureDataField), new PropertyMetadata(string.Empty, OnFieldNamePropertyChanged));

        private static void OnFieldNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = ((FeatureDataField)d);
            form.Refresh();
            form.OnPropertyChanged("FieldName");
        }

        #endregion FieldName

        #region IsReadOnly

        /// <summary>
        /// Gets or sets a value indicating whether the UI will be readonly. 
        /// If IsReadOnly is true then the UI generated will use the ReadOnlyTemplate. 
        /// Any field that is not readonly can be made readonly, but field that are 
		/// readonly already as defined by thier FieldInfo entry in GeodatabaseFeature.Schema.Fields 
        /// cannot be made editable. 
        /// </summary>        
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for IsReadOnly.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(FeatureDataField), new PropertyMetadata(false, OnIsReadOnlyPropertyChanged));

        private static void OnIsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = ((FeatureDataField)d);
            form.Refresh();
            form.OnPropertyChanged("IsReadOnly");
        }

        #endregion IsReadOnly

        #region BindingValue

        /// <summary>
        /// Gets or sets the binding value.
        /// </summary>        
        public object BindingValue
        {
            get { return GetValue(BindingValueProperty); }
            set { SetValue(BindingValueProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for BindingValue.
        /// </summary>
        public static readonly DependencyProperty BindingValueProperty =
            DependencyProperty.Register("BindingValue", typeof(object), typeof(FeatureDataField), new PropertyMetadata(null, OnBindingValuePropertyChanged));
        
        private static void OnBindingValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {            
            var field = (FeatureDataField)d;

            // if require information is missing return.
			if (string.IsNullOrEmpty(field.FieldName) || field.GeodatabaseFeature == null
				|| field.GeodatabaseFeature.Attributes == null)
            {
                field.ValidationException = null;
                return;
            }

#if !NETFX_CORE
            // WPF will raise this event even if the new value 
            // is same as old value. WinRT and WP8 will only 
            // raise this event if the new value is not the same
            // as the old value. This code is to enusre equality 
            // in behavior accross all platforms.
            if (AreEqual(e.NewValue, e.OldValue))
            {
                // clear out any previous validation exception.
                field.ValidationException = null;
                return;
            }
            
#endif                        
            // Update the UI controls data context object to the current value.
            if (field._dataItem != null && !AreEqual(field._dataItem.Value,e.NewValue))
            {
                field._dataItem.Value = (field._dataItem is SelectorDataItem) 
                    ? ((SelectorDataItem) field._dataItem).SelectItem(e.NewValue) 
                    : field._dataItem.Value = e.NewValue;                
            }

			var oldValue = field.GeodatabaseFeature.Attributes.ContainsKey(field.FieldName)
				? field.GeodatabaseFeature.Attributes[field.FieldName]
                : null;
            
            if ( AreEqual(oldValue, field.BindingValue))
            {
                // clear out any previous validation exception.
                field.ValidationException = null;
                return;            
            }
                            
            // if value changing event is subscribed to raise event.
            if (field.ValueChanging != null)
            {
                var changing = new ValueChangingEventArgs(oldValue, field.BindingValue);

                // raise value changing event.
                field.ValueChanging(field, changing);

                // if ValueChangeEventArgs return with the validation exception property
                // set then the user has indicated that the new value doesn't not meet
                // some user defined validation requirement and the validation state
                // should be trigger with the provided Exception.
                if (changing.ValidationException != null)
                {
                    // Set the users exception to the FeatureDataField.ValidationException property.
                    // This will trigger the validation state.
                    field.ValidationException = changing.ValidationException;
                    return;
                }
            }

#if NETCORE_FX
            // clear out any previous validation exception.
            form.ValidationException = null;
#endif

			// Attempt to update the new value back to the GeodatabaseFeature
            var success = field.CommitChange(field.BindingValue);

            // if the ValueChanged event is subscribed to and the committed value was 
			// successfully pushed back to the GeodatabaseFeature then rais the ValueChanged Event.
            if (field.ValueChanged != null && success)
                field.ValueChanged(field, new ValueChangedEventArgs(oldValue, field.BindingValue));

            field.OnPropertyChanged("BindingValue");
        }

        #endregion BindingValue    

        #region ValidationException

        /// <summary>
        /// Gets or sets the validation exception. If a validation exception occurs this 
        /// property will hold the validation Exception. default validation is handled based
		/// on FieldInfo found inside GeodatabaseFeature.Schema.Fields. Custom validation can be added
        /// by subsribing to ValueChanging event.
        /// </summary>     
        public Exception ValidationException
        {
            get { return (Exception)GetValue(ValidationExceptionProperty); }
            set { SetValue(ValidationExceptionProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for ValidationException.
        /// </summary>
        public static readonly DependencyProperty ValidationExceptionProperty =
            DependencyProperty.Register("ValidationException", typeof(Exception), typeof(FeatureDataField), new PropertyMetadata(null, OnValidationExceptionPropertyChanged));

        private static void OnValidationExceptionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = ((FeatureDataField)d);
            form.UpdateValidationState();
            form.OnPropertyChanged("ValidationException");            
        }          

        #endregion ValidationException

        #region SelectorTemplate

        /// <summary>
        /// Gets or sets the selector template. Selector template is used for fields 
        /// that are CodeValueDomain types as define in FieldInfo.Domain.
        /// </summary>     
        public DataTemplate SelectorTemplate
        {
            get { return (DataTemplate)GetValue(SelectorTemplateProperty); }
            set { SetValue(SelectorTemplateProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for SelectorTemplate.
        /// </summary>
        public static readonly DependencyProperty SelectorTemplateProperty =
            DependencyProperty.Register("SelectorTemplate", typeof(DataTemplate), typeof(FeatureDataField), new PropertyMetadata(null, OnSelectorTemplatePropertyChanged));

        private static void OnSelectorTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = ((FeatureDataField)d);
            form.Refresh();
            form.OnPropertyChanged("SelectorTemplate");
        }

        #endregion SelectorTemplate

        #region InputTemplate

        /// <summary>
        /// Gets or sets the input template. This template is used for 
        /// fields that do not have CodedValueDomain information.
        /// </summary>       
        public DataTemplate InputTemplate
        {
            get { return (DataTemplate)GetValue(InputTemplateProperty); }
            set { SetValue(InputTemplateProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for InputTemplate.
        /// </summary>
        public static readonly DependencyProperty InputTemplateProperty =
            DependencyProperty.Register("InputTemplate", typeof(DataTemplate), typeof(FeatureDataField), new PropertyMetadata(null, OnInputTemplatePropertyChanged));

        private static void OnInputTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = ((FeatureDataField)d);
            form.Refresh();
            form.OnPropertyChanged("InputTemplate");
        }

        #endregion InputTemplate
        
        #region ReadOnlyTemplate

        /// <summary>
        /// Gets or sets the read only template. This template is used for fields
        /// that are readonly. 
        /// </summary>      
        public DataTemplate ReadOnlyTemplate
        {
            get { return (DataTemplate)GetValue(ReadOnlyTemplateProperty); }
            set { SetValue(ReadOnlyTemplateProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for ReadOnlyTemplate.
        /// </summary>
        public static readonly DependencyProperty ReadOnlyTemplateProperty =
            DependencyProperty.Register("ReadOnlyTemplate", typeof(DataTemplate), typeof(FeatureDataField), new PropertyMetadata(null, OnReadOnlyTemplatePropertyChanged));

        private static void OnReadOnlyTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = ((FeatureDataField)d);
            form.Refresh();
            form.OnPropertyChanged("ReadOnlyTemplate");
        }

        #endregion ReadOnlyTemplate               
                                
        #region  TextBoxChangedListener

        /// <summary>
        /// *FOR INTERNAL USE ONLY* : Gets the text box changed listener.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <exclude/>        
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TextBox GetTextBoxChangedListener(DependencyObject obj)
        {
            return (TextBox)obj.GetValue(TextBoxChangedListenerProperty);
        }

        /// <summary>
        /// *FOR INTERNAL USE ONLY* : Sets the text box changed listener.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">The value.</param>
        /// <exclude/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetTextBoxChangedListener(DependencyObject obj, TextBox value)
        {
            obj.SetValue(TextBoxChangedListenerProperty, value);
        }

        /// <summary>
        /// *FOR INTERNAL USE ONLY* : The text box changed listener property
        /// </summary>
        /// <exclude/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty TextBoxChangedListenerProperty =
            DependencyProperty.RegisterAttached("TextBoxChangedListener", typeof(TextBox), typeof(FeatureDataField), new PropertyMetadata(null, OnTextBoxChangedListenerPropertyChanged));

        private static void OnTextBoxChangedListenerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                var txtbox = ((TextBox)e.OldValue);
                txtbox.TextChanged -= txtbox_TextChanged;
            }

            if (e.NewValue != null)
            {
                var txtbox = ((TextBox) e.NewValue);
                txtbox.TextChanged += txtbox_TextChanged;
            }
        }

       private static void txtbox_TextChanged(object sender, TextChangedEventArgs e)
       {
            var txtbox = (TextBox)sender;                       
            var textbinding = txtbox.GetBindingExpression(TextBox.TextProperty);
            if (textbinding != null)
                textbinding.UpdateSource();
        }

        #endregion TextBoxChangedListener

        #endregion Public Fields

        #region Events

        /// <summary>
        /// This event is rasied when the Value property changes but 
	   /// has not been commit back to the GeodatabaseFeature. The ValueChanging 
        /// event can be used to enforce application validation setting the 
        /// ValueChangingEventArgs.ValidationException.
        /// </summary>
        public event EventHandler<ValueChangingEventArgs> ValueChanging;

        /// <summary>
        /// This event is rasied when the Value property changes and the value
		/// has been successfully commit back to the GeodatabaseFeature.
        /// </summary>
        public event EventHandler<ValueChangedEventArgs> ValueChanged;        

        #endregion Events

        #region Override

#if NETFX_CORE
        /// <summary>
        /// Invoked whenever application code or internal processes 
        /// (such as a rebuilding layout pass) call ApplyTemplate. 
        /// In simplest terms, this means the method is called just 
        /// before a UI element displays in your app. Override this 
        /// method to influence the default post-template logic of 
        /// a class.
        /// </summary>
        protected
#else
        /// <summary>
        /// When overridden in a derived class, is invoked whenever application 
        /// code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public
#endif
        override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // objain require templated controls
            _contentControl = GetTemplateChild("FeatureDataField_ContentControl") as ContentControl;

            if (_contentControl != null)
            {
                _contentControl.GotFocus += ContentControl_GotFocus;
                _contentControl.LostFocus += ContentControl_LostFocus;                     
            }            

            // Render the UI.
            Refresh();
        }      

        private void ContentControl_GotFocus(object sender, RoutedEventArgs e)
        {
            _focused = true;            
            UpdateValidationState();
        }

        private void ContentControl_LostFocus(object sender, RoutedEventArgs e)
        {            
            _focused = false;
            UpdateValidationState();
        }      

        #endregion Override        

        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region Private Methods

        /// <summary>
        /// This method is used to create the UI for the FeatureDataField.
        /// </summary>
        private void Refresh()
        {
            // if the content control template part is missing then draw nothing.
            if (_contentControl == null)
                return;

            // attempt retrive the field info for our control
			_fieldInfo = GeodatabaseFeature.GetFieldInfo(FieldName);
            
            // if field information was not obtain then draw nothing.
            if (_fieldInfo == null) return;

			// Get the value from the GeodatabaseFeature if the attribute exists.
			BindingValue = GeodatabaseFeature.Attributes.ContainsKey(FieldName) ? GeodatabaseFeature.Attributes[FieldName] : null;

            // If the FeatureDataField.IsReadOnly property has been set to true or the FieldInfo 
            if (IsReadOnly || !_fieldInfo.IsEditable)
            {
                switch (_fieldInfo.Type)
                {
                    case FieldType.String:                                                
                    case FieldType.Double:
                    case FieldType.Single:
                    case FieldType.SmallInteger:
                    case FieldType.Integer:                                                
                    case FieldType.Date:                                                
                    case FieldType.GlobalID:
                    case FieldType.Guid:
                    case FieldType.Oid:
                        GenerateReadonlyField();        
                        break;
                    case FieldType.Raster:
                    case FieldType.Geometry:
                    case FieldType.Unknown:
                    case FieldType.Xml:
                    case FieldType.Blob:
                        Visibility = Visibility.Collapsed;
                        break;
                }               
            }
            else if (_fieldInfo.Domain is CodedValueDomain)
            {
                // Create selector UI
                GenerateSelectorField();                
            }
            else
            {
                switch (_fieldInfo.Type)
                {
                    case FieldType.String:
                        GenerateInputField();
                        break;
                    case FieldType.Double:
                    case FieldType.Single:
                    case FieldType.SmallInteger:
                    case FieldType.Integer:                    
                        GenerateInputField();
                        break;                                                                            
                    case FieldType.Date:
                        GenerateInputField();
                        break;
                    case FieldType.GlobalID:
                    case FieldType.Guid:
                    case FieldType.Oid:
                        GenerateReadonlyField();                                                    
                        break;
                    case FieldType.Raster:                        
                    case FieldType.Geometry:
                    case FieldType.Unknown:
                    case FieldType.Xml:
                    case FieldType.Blob:
                        Visibility = Visibility.Collapsed;
                        break;
                }                
            }
        }      

        /// <summary>
        /// Creates the SelectorTemplate.
        /// </summary>
        private void GenerateSelectorField()
        {                                    
            var values = new List<KeyValuePair<object, string>>();

            var cvd = _fieldInfo.GetCodedValueDomain();
            if (cvd != null)
            {
                if (_fieldInfo.IsNullable)
                    values.Add(new KeyValuePair<object, string>(null, ""));
#if !NETFX_CORE
                values.AddRange(cvd.CodedValues);                
#else
                values.AddRange(cvd.CodedValues.Select(kvp => new KeyValuePair<object, string>(kvp.Key, kvp.Value)));
#endif
            }

            if (_contentControl == null) return;
            _contentControl.ContentTemplate = SelectorTemplate;
            _dataItem = new SelectorDataItem(values, ValueChangedCallback, BindingValue);
            _contentControl.Content = _dataItem;
        }       

        /// <summary>
        /// Creates the InputTemplate.
        /// </summary>
        private void GenerateInputField()
        {            
            _contentControl.ContentTemplate = InputTemplate;
            _dataItem = new InputDataItem(ValueChangedCallback, _fieldInfo, BindingValue);
            _contentControl.Content = _dataItem;
        }

        /// <summary>
        /// Create the ReadOnlyTemplate.
        /// </summary>
        private void GenerateReadonlyField()
        {           
            if (_contentControl == null) return;
            _contentControl.ContentTemplate = ReadOnlyTemplate;
            _dataItem = new ReadOnlyDataItem(_fieldInfo, BindingValue);
            _contentControl.Content = _dataItem;
        }
        
        /// <summary>
        ///  Callback used to track when the value changes in one of the DataItem classes.
        /// </summary>
        /// <param name="value">new value entered or selected by user.</param>
        private void ValueChangedCallback(object value)        
        {
            // Update Value with DataItem value
            BindingValue = value;
        }

        /// <summary>
        /// Tries to make sure that the value can be converted to the correct datatype if
        /// the input control works with a differnt datatype. i.e. Textbox works with string data.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>Either a corrected data type or the input value.</returns>
        private object EnsureCorrectDataType(object value)
        {
            if (value == null)
                return null;

            switch (_fieldInfo.Type)
            {
                case FieldType.String:
                    return value;
                case FieldType.Double:
                    try
                    {
                        value = EmptyStringToNull(value);
                        return (value != null) ? (object) Convert.ToDouble(value) : null;
                    } 
                    catch { return value; }
                case FieldType.Integer:
                case FieldType.Oid:
                    try
                    {
                        value = EmptyStringToNull(value);
                        return (value != null) ? (object)Convert.ToInt32(value) : null;
                    }
                    catch { return value; }                    
                case FieldType.Single:
                    try
                    {
                        value = EmptyStringToNull(value);
                        return (value != null) ? (object) Convert.ToSingle(value) : null;
                    }
                    catch { return value; }                    
                case FieldType.SmallInteger:
                    try
                    {
                        value = EmptyStringToNull(value);
                        return (value != null) ? (object) Convert.ToInt16(value) : null;
                    }
                    catch { return value; }                    
                case FieldType.Date:
                    try
                    {
                        value = EmptyStringToNull(value);
                        if (value == null) return null;

                        DateTime dt;                       
                        if (value is string)                            
                            return DateTime.TryParse((string)value, out dt) ? dt : value;
                        return value;
                    }
                    catch { return value; }                                                             
                default:
                    return value;;
            }
        }

        private object EmptyStringToNull(object value)
        {
            if (value is string && string.IsNullOrEmpty((string)value))
                return null;
            return value;
        }

        /// <summary>
        /// Changes the validation state from betwenn Invalid and Valid.
        /// </summary>
        private void UpdateValidationState()
        {        
            _dataItem.ErrorMessage = ValidationException != null ? ValidationException.Message : "";

            var deltaState = ValidationException != null ? _focused ? "InvalidFocusedState" : "InvalidUnfocusedState" : "ValidState";

            if (_currentState != deltaState)
            {
                _currentState = deltaState;
                GoToState(_contentControl, _currentState);
            }

        }

        /// <summary>
        /// Helper method to parse the visual tree of the ContentControl to look VisualStatue that 
        /// are defined for ValidationStates category. 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool GoToState(DependencyObject element, string state )
        {            
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            if (childCount <= 0) return false;
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                if ((child is Control) && VisualStateManager.GoToState((Control)child, state, true))
                    return true;
                if (child is FrameworkElement && TryValidationState((FrameworkElement)child, state))
                    return true;
                if (GoToState(child, state))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to look for a VisualStateGroup called "ValidationState" and invoke the 
        /// Storyboard for the given visual state.
        /// </summary>
        /// <param name="element">The FrameworkElement to check for VisualStates</param>
        /// <param name="stateName">The name of the validation state we will try to invoke.</param>
        /// <returns>returns true if the storyboard was found and invoked.</returns>
        private bool TryValidationState(FrameworkElement element, string stateName)
        {
            var groups = VisualStateManager.GetVisualStateGroups(element);
            if (groups == null) return false;
            foreach (var state in from VisualStateGroup @group in groups 
                                  where @group.Name == "ValidationStates" 
                                  from VisualState state in @group.States 
                                  where state.Name == stateName select state)
            {
                state.Storyboard.Begin();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Changes the current value back to 
		/// the GeodatabaseFeature last saved value.
        /// </summary>
        private void Cancel()
        {
            if (string.IsNullOrEmpty(FieldName)
				|| GeodatabaseFeature == null
				|| GeodatabaseFeature.Attributes == null
				|| !GeodatabaseFeature.Attributes.ContainsKey(FieldName))
                return;           

            // Clear validation exception
            ValidationException = null;

			// Take current GeodatabaseFeature value and override Value.
			BindingValue = GeodatabaseFeature.Attributes[FieldName];
        }

        /// <summary>
		/// Commits FeatureDataField value back to the GeodatabaseFeature.
        /// </summary>
        /// <param name="value">The new value that will be committed.</param>
        /// <returns>returns true if the value was commited and false if the 
        /// value didn't pass validation.</returns>
        private bool CommitChange(object value)
        {
            try
            {  
                // try to commit value
				GeodatabaseFeature.Attributes[FieldName] = EnsureCorrectDataType((value is KeyValuePair<object, string>) ? ((KeyValuePair<object, string>)value).Key : value);
                ValidationException = null;
                return true; // commit success
            }
            catch (Exception ex)
            {                
                ValidationException = ex; // raise validation state.
                return false; // commit failed.
            }
            
        }

        // Simplified equality test that handles 
        // nulls, values, and reference
        private static bool AreEqual(object o1, object o2)
        {
            return (o1 == o2) || (o1 != null && o1.Equals(o2));
        }
        
        #endregion Private Methods     

        #region DataItem

        /// <summary>
        /// Abstract base class for all DataItem derived class. They all should have 
        /// a Value property and notify the FeatureDataField when thier value changes.
        /// </summary>
        private abstract class DataItem : INotifyPropertyChanged
        {
            #region Private Members
            
            private object _value;
            private string _errorMessage;

            #endregion Private Members           

            #region Constructors

            /// <summary>
            /// Creates new instance of DataItem and sets the callback property.
            /// </summary>
            /// <param name="callback">The callback to be raised 
            /// when Value proeprty changes.</param>
            protected DataItem(Action<object> callback)
            {
                Callback = callback;
            }

            /// <summary>
            /// Creates new instance of DataItem and sets the callback and value property.
            /// </summary>
            /// <param name="callback">The callback to be raised 
            /// when Value proeprty changes.</param>
            /// <param name="value">The default value.</param>
            protected DataItem(Action<object> callback, object value) : this (callback)
            {             
                Value = value;
            }

            #endregion Constructors

            #region Public Properties

            /// <summary>
            /// Callback for notifying FeatureDataField that the value proepry has changed.
            /// </summary>
            protected readonly Action<object> Callback;

            #endregion Public Properties

            #region Virutal Methods

            /// <summary>
            /// Rasies the callback and passes the new value.
            /// </summary>
            protected virtual void OnValueChanged()
            {
                if (Callback != null)
                    Callback(Value);
            }

            #endregion Virtual Methods

            #region Virtual Properties

            /// <summary>
            /// The Value property to bind ContentTemplate.
            /// </summary>
            public virtual object Value
            {
                get { return _value; }
                set
                {
                    if (_value == null && value == null) return;
                    if (_value != null && _value.Equals(value)) return;
                    _value = value;
                    OnPropertyChanged();
                    OnValueChanged();
                }
            }

            /// <summary>
            /// Gets or sets the error message.
            /// </summary>           
            public virtual string ErrorMessage
            {
                get { return _errorMessage; }
                set
                {
                    if (_errorMessage == value) return;
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }

            #endregion Virtual Properties

            #region INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged;
            
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion INotifyPropertyChanged
        }

        #endregion DataItem

        #region ReadOnlyDataItem

        /// <summary>
        /// ReadOnlyDataItem is a bindable object for ReadOnlyTeample.
        /// </summary>
        private class ReadOnlyDataItem : DataItem
        {
            #region Private Members

            private object _value;
            private readonly FieldInfo _fieldInfo;

            #endregion Private Members                        

            #region Constructors

            /// <summary>
            /// Creates a new instance of ReadOnlyDataItem and sets 
            /// the FieldInfo and Value properties.
            /// </summary>
            /// <param name="fileInfo">the field information for the </param>
            /// <param name="value">default value.</param>
            public ReadOnlyDataItem(FieldInfo fileInfo, object value) 
                : base(null, value)
            {
                _fieldInfo = fileInfo;                
            }

            #endregion Constructors

            #region Public Properties

            /// <summary>
            /// The current value for the ReadOnlyTemplate to display.
            /// </summary>
            public override object Value
            {
                get { return GetReadonlyValue(); }
                set
                {
                    if (_value == null && value == null) return;
                    if (_value != null && _value.Equals(value)) return;
                    _value = value;
                    OnPropertyChanged();                    
                }
            }

            #endregion Public Properties

            /// <summary>
            /// Gets the display value.
            /// </summary>            
            private object GetReadonlyValue()
            {                    
                // if field is coded value lookup the value
                var cvd = _fieldInfo.GetCodedValueDomain();
                return cvd != null ? _fieldInfo.GetDisplayValue(_value) : _value;
            }                
        }

        #endregion ReadOnlyDataItem

        #region InputDataItem

        /// <summary>
        /// InputDataItem is a bindable object for InputTemplate.
        /// </summary>
        private class InputDataItem : DataItem
        {
            private FieldInfo _fieldInfo;
            
            public InputDataItem(Action<object> callback, FieldInfo fieldInfo, object value) 
                : base(callback, value)
            {
                FieldInfo = fieldInfo;                
            }

            public FieldInfo FieldInfo
            {
                get { return _fieldInfo; }
                set
                {
                    if (_fieldInfo != null && _fieldInfo == value) return;
                    _fieldInfo = value;
                    OnPropertyChanged();
                    OnPropertyChanged("Value");
                }
            }           
        }

        #endregion InputDataItem

        #region SelectorDataItem

        /// <summary>
        /// SelectorDataItem is used as the binding object for the SelectorTemplate.
        /// </summary>
#if NETFX_CORE
        [Bindable]
#endif
        private class SelectorDataItem : DataItem
        {
            #region  Private Members

            private List<KeyValuePair<object,string>> _items;

            #endregion Private Members

            #region Consturctor

            /// <summary>
            /// Constructor that creates SelectorDataItem.
            /// </summary>
            /// <param name="items">Coded value domain options</param>
            /// <param name="callback">When value changes a callback to notify of a value change.</param>
            /// <param name="value">The default value which should be selected.</param>
            public SelectorDataItem(IEnumerable<KeyValuePair<object, string>> items, Action<object> callback, object value) : base(callback)
            {
                Items = items != null ? items.ToList() : null;
                Value = SelectItem(value);
            }

            /// <summary>
            /// Looks for and item entry that has the matching key for the given input value.
            /// </summary>
            /// <param name="value">returns a valid selector object</param>            
            public KeyValuePair<object,string> SelectItem(object value)
            {
                return Items.FirstOrDefault(kvp => ((kvp.Key == null && value == null) || (kvp.Key != null && kvp.Key.Equals(value))));
            }

            #endregion Constructor

            #region Public Properties

            /// <summary>
            /// The Items used to bind to the selector template.
            /// </summary>
            public List<KeyValuePair<object,string>> Items
            {
                get { return _items; }
                set
                {
                    if (_items == value) return;
                    _items = value;
                    OnValueChanged();
                }
            }

            #endregion Public properties

            #region Protected Method

            /// <summary>
            /// When value changes raised the callback with the selected value.
            /// </summary>
            protected override void OnValueChanged()
            {
                if (Items == null) return;
                var selector = (from a in Items where (Value != null && AreEqual(a.Key,((KeyValuePair<object,string>)Value).Key))  select a.Key);
                var enumerable = selector as string[] ?? selector.ToArray();
                
                if (enumerable.Any() && Callback != null)                
                    Callback(enumerable.First());
            }

            #endregion Protected Method
        }

        #endregion SelectorDataItem
        
    }
}