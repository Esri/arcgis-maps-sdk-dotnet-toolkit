using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    /// <summary>
    /// FeatureDatafield is used to edit or display a single attribute from a GdbFeature.
    /// </summary>
    [TemplatePart(Name = "FeatureDataField_ContentControl", Type = typeof(ContentControl))]
    public sealed class FeatureDataField : Control
    {       
        #region Private Properties

        private ContentControl _contentControl;               
        private FieldInfo _fieldInfo;
        private DataItem _dataItem;        

        #endregion Private Properties

        #region Constructor

#if NETFX_CORE || WINDOWS_PHONE
        public FeatureDataField()
        {
            DefaultStyleKey = typeof(FeatureDataField);
        }
#else
        static FeatureDataField()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FeatureDataField), new FrameworkPropertyMetadata(typeof(FeatureDataField)));
        }
#endif
        #endregion Constructor

        #region Public Fields

        #region GdbFeature

        /// <summary>
        /// Gets or sets the GdbFeature.
        /// </summary>        
        public GdbFeature GdbFeature
        {
            get { return (GdbFeature)GetValue(GdbFeatureProperty); }
            set { SetValue(GdbFeatureProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for GdbFeature.
        /// </summary>
        public static readonly DependencyProperty GdbFeatureProperty =
            DependencyProperty.Register("GdbFeature", typeof(GdbFeature), typeof(FeatureDataField), new PropertyMetadata(null, OnGdbFeaturePropertyChanged));

        private static void OnGdbFeaturePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FeatureDataField)d).Refresh();            
        }

        #endregion GdbFeature

        #region FieldName

        /// <summary>
        /// Gets or sets the name of the field from the GdbFeature.Attributes. 
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
            ((FeatureDataField)d).Refresh();
        }

        #endregion FieldName

        #region IsReadOnly

        /// <summary>
        /// Gets or sets a value indicating whether the UI will be readonly. 
        /// If IsReadOnly is true then the UI generated will use the ReadOnlyTemplate. 
        /// Any field that is not readonly can be made readonly, but field that are 
        /// readonly already as defined by thier FieldInfo entry in GdbFeature.Schema.Fields 
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
            ((FeatureDataField)d).Refresh();
        }

        #endregion IsReadOnly

        #region Value

        /// <summary>
        /// Gets or sets the value.
        /// </summary>        
        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for Value.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(FeatureDataField), new PropertyMetadata(null, OnValuePropertyChanged));
        
        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var f = (FeatureDataField)d;            
#if !NETFX_CORE
            // WPF will raise this event even if the new value 
            // is same as old value. WinRT and WP8 will only 
            // raise this event if the new value is not the same
            // as the old value. This code is to enusre equality 
            // in behavior accross all platforms.
            if (e.NewValue == e.OldValue) return; 
#endif            

            // Update the UI controls data context object to the current value.
            if (f._dataItem != null)
                f._dataItem.Value = e.NewValue;

            // If the FeatureDataField doesn't have the information require to update
            // the GdbFeature or the FeatureDataField is initalizing itself do not raise changing or changed event.
            if (string.IsNullOrEmpty(f.FieldName) || f.GdbFeature == null 
                || f.GdbFeature.Attributes == null || !f.GdbFeature.Attributes.ContainsKey(f.FieldName)
                || f.GdbFeature.Attributes[f.FieldName] == e.NewValue)
                return;            


            // if value changing event is subscribed to raise event.
            if (f.ValueChanging != null) 
            {
                var changing = new ValueChangingEventArgs(e.OldValue, e.NewValue);
                
                // raise value changing event.
                f.ValueChanging( f ,changing);

                // if ValueChangeEventArgs return with the validation exception property
                // set then the user has indicated that the new value doesn't not meet
                // some user defined validation requirement and the validation state
                // should be trigger with the provided Exception.
                if(changing.ValidationException != null)
                {
                    // Set the users exception to the FeatureDataField.ValidationException property.
                    // This will trigger the validation state.
                    f.ValidationException = changing.ValidationException;
                    return;
                }                    
            }

            // clear out any previous validation exception.
            f.ValidationException = null;

            // Attempt to update the new value back to the GdbFeature
            bool success = f.CommitChange(e.NewValue);

            // if the ValueChanged event is subscribed to and the committed value was 
            // successfully pushed back to the GdbFeatue then rais the ValueChanged Event.
            if (f.ValueChanged != null && success)
                f.ValueChanged( f , new ValueChangedEventArgs(e.OldValue, e.NewValue));

        }

        #endregion Value    

        #region ValidationException

        /// <summary>
        /// Gets or sets the validation exception. If a validation exception occurs this 
        /// property will hold the validation Exception. default validation is handled based
        /// on FieldInfo found inside GdbFeature.Schema.Fields. Custom validation can be added
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
            ((FeatureDataField)d).UpdateValidationState();            
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
            ((FeatureDataField)d).Refresh();
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
            ((FeatureDataField)d).Refresh();
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
            ((FeatureDataField)d).Refresh();
        }

        #endregion ReadOnlyTemplate       

        #endregion Public Fields

        #region Events

        /// <summary>
        /// This event is rasied when the Value property changes but 
        /// has not been commit back to the GdbFeature. The ValueChanging 
        /// event can be used to enforce application validation setting the 
        /// ValueChangingEventArgs.ValidationException.
        /// </summary>
        public EventHandler<ValueChangingEventArgs> ValueChanging;

        /// <summary>
        /// This event is rasied when the Value property changes and the value
        /// has been successfully commit back to the GdbFeature.
        /// </summary>
        public EventHandler<ValueChangedEventArgs> ValueChanged;

        #endregion Events

        #region Override

#if NETFX_CORE
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

            // Render the UI.
            Refresh();
        }

        #endregion Override        

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
            _fieldInfo = GdbFeature.GetFieldInfo(FieldName);
            
            // if field information was not obtain then draw nothing.
            if (_fieldInfo == null) return;
            
            // Get the value from the GdbFeature if the attribute exists.
            Value = GdbFeature.Attributes.ContainsKey(FieldName) ? GdbFeature.Attributes[FieldName] : null;

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
                        this.Visibility = Visibility.Collapsed;
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
                        this.Visibility = Visibility.Collapsed;
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
                values.AddRange(cvd.CodedValues);
            }

            if (_contentControl == null) return;
            _contentControl.ContentTemplate = SelectorTemplate;
            _dataItem = new SelectorDataItem(values, ValueChangedCallback, Value);
            _contentControl.Content = _dataItem;
        }        

        /// <summary>
        /// Creates the InputTemplate.
        /// </summary>
        private void GenerateInputField()
        {            
            _contentControl.ContentTemplate = InputTemplate;
            _contentControl.Content = new InputDataItem(ValueChangedCallback, _fieldInfo, Value);
        }

        /// <summary>
        /// Create the ReadOnlyTemplate.
        /// </summary>
        private void GenerateReadonlyField()
        {           
            if (_contentControl == null) return;
            _contentControl.ContentTemplate = ReadOnlyTemplate;
            _contentControl.Content = new ReadOnlyDataItem(_fieldInfo, Value);
        }
        
        /// <summary>
        ///  Callback used to track when the value changes in one of the DataItem classes.
        /// </summary>
        /// <param name="value">new value entered or selected by user.</param>
        private void ValueChangedCallback(object value)        
        {
            // Attempt to convert text value into correct data type.
            Value = EnsureCorrectDataType(value);
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
                    try { return Convert.ToDouble(value); } 
                    catch { return value; }
                case FieldType.Integer:
                case FieldType.Oid:
                    try { return Convert.ToInt32(value); }
                    catch { return value; }                    
                case FieldType.Single:
                    try { return Convert.ToSingle(value); }
                    catch { return value; }                    
                case FieldType.SmallInteger:
                    try { return Convert.ToInt16(value); }
                    catch { return value; }                    
                case FieldType.Date:
                    try
                    {
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

        /// <summary>
        /// Changes the validation state from betwenn Invalid and Valid.
        /// </summary>
        private void UpdateValidationState()
        {            
            GoToState(_contentControl, ValidationException != null ? "InvalidState" : "ValidState");            
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
        /// Commits FeatureDataField value back to the GdbFeature.
        /// </summary>
        /// <param name="value">The new value that will be committed.</param>
        /// <returns>returns true if the value was commited and false if the 
        /// value didn't pass validation.</returns>
        private bool CommitChange(object value)
        {
            try
            {  
                // try to commit value
                GdbFeature.Attributes[FieldName] = (value is KeyValueItem) ? ((KeyValueItem)value).Key : value;
                return true; // commit success
            }
            catch (Exception ex)
            {                
                ValidationException = ex; // raise validation state.
                return false; // commit failed.
            }
            
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
        private class SelectorDataItem : DataItem
        {
            #region  Private Members

            private List<KeyValueItem> _items;

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
                Items = items.Select(kvp => new KeyValueItem() { Key = kvp.Key, Value = kvp.Value }).ToList();
                Value = Items.FirstOrDefault(kvp => ((kvp.Key == null && value == null) || (kvp.Key != null && kvp.Key.Equals(value))));
            }

            #endregion Constructor

            #region Public Properties

            /// <summary>
            /// The Items used to bind to the selector template.
            /// </summary>
            public List<KeyValueItem> Items
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
                var selector = (from a in Items where ((a.Key == null && Value == null) || (a.Key != null && a.Key.Equals(Value)))  select a.Key);
                var enumerable = selector as string[] ?? selector.ToArray();
                
                if (enumerable.Any() && Callback != null)                
                    Callback(enumerable.First());
            }

            #endregion Protected Method
        }

        #endregion SelectorDataItem
    }
}
