// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if !XAMARIN

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#endif
using Esri.ArcGISRuntime.Data;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The FeatureDataField control is used to display or edit a single field attribute of an <see cref="Feature"/>.
    /// </summary>
    public class FeatureDataField : Control
    {
        private ContentControl _contentControl;
        private Field _field;
        private DataItem _dataItem;
        private bool _focused;
        private string _currentState;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureDataField"/> class.
        /// </summary>
        public FeatureDataField()
        {
            DefaultStyleKey = typeof(FeatureDataField);
        }

        /// <inheritdoc/>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            _contentControl = GetTemplateChild("FeatureDataField_ContentControl") as ContentControl;
            if (_contentControl != null)
            {
                _contentControl.GotFocus += ContentControl_GotFocus;
                _contentControl.LostFocus += ContentControl_LostFocus;
            }

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

        /// <summary>
        /// Gets or sets the <see cref="Data.Feature">Feature</see> that owns the field attribute the schema and values of fields.
        /// </summary>
        public Feature Feature
        {
            get { return (Feature)GetValue(FeatureProperty); }
            set { SetValue(FeatureProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Feature"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FeatureProperty =
            DependencyProperty.Register(nameof(Feature), typeof(Feature), typeof(FeatureDataField), new PropertyMetadata(null, OnFeaturePropertyChanged));

        private static void OnFeaturePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;
            featureDataField.Refresh();
        }

        /// <summary>
        /// Gets or sets the name of the attribute field that will be used to generate control.
        /// </summary>
        public string FieldName
        {
            get { return (string)GetValue(FieldNameProperty); }
            set { SetValue(FieldNameProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FieldName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(FeatureDataField), new PropertyMetadata(null, OnFieldNamePropertyChanged));

        private static void OnFieldNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;
            featureDataField.Refresh();
        }

        /// <summary>
        /// Gets or sets a value indicating whether generated control will be read-only.
        /// </summary>
        /// <remarks>
        /// Any field that is editable as defined by the feature schema can be made read-only.
        /// Any field that is read-only as defined by the feature schema cannot be made editable.
        /// </remarks>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(FeatureDataField), new PropertyMetadata(false, OnIsReadOnlyPropertyChanged));

        private static void OnIsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;
            featureDataField.Refresh();
        }

        /// <summary>
        /// Gets or sets the attribute value for field.
        /// </summary>
        public object BindingValue
        {
            get { return (object)GetValue(BindingValueProperty); }
            set { SetValue(BindingValueProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="BindingValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BindingValueProperty =
            DependencyProperty.Register(nameof(BindingValue), typeof(object), typeof(FeatureDataField), new PropertyMetadata(null, OnBindingValuePropertyChanged));

        private static void OnBindingValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;

            // Clear out previous validation exception
            featureDataField.ClearValue(ValidationExceptionProperty);

            if (string.IsNullOrEmpty(featureDataField.FieldName) || featureDataField.Feature?.Attributes == null
                || !featureDataField.Feature.Attributes.ContainsKey(featureDataField.FieldName)
                || featureDataField._dataItem == null)
            {
                return;
            }

            // Update the UI controls data context object to the current value.
            if (featureDataField._dataItem.Value == null || !featureDataField._dataItem.Value.Equals(e.NewValue))
            {
                featureDataField._dataItem.Value = (featureDataField._dataItem is SelectorDataItem)
                    ? ((SelectorDataItem)featureDataField._dataItem).GetSelectedItem(e.NewValue)
                    : e.NewValue;
            }

            // Use the last saved attribute value on feature for old value.
            var oldValue = featureDataField.Feature.Attributes[featureDataField.FieldName];

            try
            {
                featureDataField.OnValueChanging(oldValue, e.NewValue);

                // Raise ValueChanging event to allow for custom validation.
                featureDataField.ValueChanging?.Invoke(featureDataField, new AttributeValueChangedEventArgs(oldValue, e.NewValue));
            }
            catch (System.Exception ex)
            {
                featureDataField.SetValue(ValidationExceptionProperty, ex);
                return;
            }

            // Commits attribute edit to feature.
            var success = featureDataField.CommitChange(e.NewValue);

            // Raise ValueChanged event on successful edit.
            if (success)
            {
                featureDataField.OnValueChanged(oldValue, e.NewValue);
                featureDataField.ValueChanged?.Invoke(featureDataField, new AttributeValueChangedEventArgs(oldValue, e.NewValue));
            }
        }

        /// <summary>
        /// Called when an attribute value is about to change.
        /// </summary>
        /// <remarks>
        /// To trigger a validation exception, throw an exception in this method.
        /// </remarks>
        /// <param name="oldvValue">The old attribute value.</param>
        /// <param name="newValue">The new updated attribute value.</param>
        /// <seealso cref="ValueChanging"/>
        protected void OnValueChanging(object oldvValue, object newValue)
        {
        }

        /// <summary>
        /// Called when an attribute value has changed.
        /// </summary>
        /// <param name="oldvValue">The old attribute value.</param>
        /// <param name="newValue">The new updated attribute value.</param>
        /// <seealso cref="ValueChanged"/>
        protected void OnValueChanged(object oldvValue, object newValue)
        {
        }

        /// <summary>
        /// Gets the validation exception.
        /// </summary>
        /// <remarks>
        /// Default validation is handled based on field schema.
        /// Custom validation may be added by subscribing to ValueChanging event.
        /// </remarks>
        public Exception ValidationException
        {
            get { return (Exception)GetValue(ValidationExceptionProperty); }
        }

        /// <summary>
        /// Identifies the <see cref="BindingValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationExceptionProperty =
            DependencyProperty.Register(nameof(ValidationException), typeof(object), typeof(FeatureDataField), new PropertyMetadata(null, OnValidationExceptionPropertyChanged));

        private static void OnValidationExceptionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;
            featureDataField.UpdateValidationState();
        }

        /// <summary>
        /// Gets or sets the template used when field is bound to a coded-value domain.
        /// </summary>
        /// <seealso cref="ReadOnlyTemplate"/>
        /// <seealso cref="InputTemplate"/>
        public DataTemplate SelectorTemplate
        {
            get { return (DataTemplate)GetValue(SelectorTemplateProperty); }
            set { SetValue(SelectorTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectorTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectorTemplateProperty =
            DependencyProperty.Register(nameof(SelectorTemplate), typeof(DataTemplate), typeof(FeatureDataField), new PropertyMetadata(null, OnSelectorTemplatePropertyChanged));

        private static void OnSelectorTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;
            featureDataField.Refresh();
        }

        /// <summary>
        /// Gets or sets the template used for providing input when field is not bound to a coded-value domain.
        /// </summary>
        /// <seealso cref="ReadOnlyTemplate"/>
        /// <seealso cref="SelectorTemplate"/>
        public DataTemplate InputTemplate
        {
            get { return (DataTemplate)GetValue(InputTemplateProperty); }
            set { SetValue(InputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="InputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InputTemplateProperty =
            DependencyProperty.Register(nameof(InputTemplate), typeof(DataTemplate), typeof(FeatureDataField), new PropertyMetadata(null, OnInputTemplatePropertyChanged));

        private static void OnInputTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;
            featureDataField.Refresh();
        }

        /// <summary>
        /// Gets or sets the template used when <see cref="IsReadOnly"/> is <c>true</c>
        /// or field is read-only as defined by field schema.
        /// </summary>
        /// <seealso cref="InputTemplate"/>
        /// <seealso cref="SelectorTemplate"/>
        public DataTemplate ReadOnlyTemplate
        {
            get { return (DataTemplate)GetValue(ReadOnlyTemplateProperty); }
            set { SetValue(ReadOnlyTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ReadOnlyTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ReadOnlyTemplateProperty =
            DependencyProperty.Register(nameof(ReadOnlyTemplate), typeof(DataTemplate), typeof(FeatureDataField), new PropertyMetadata(null, OnReadOnlyTemplatePropertyChanged));

        private static void OnReadOnlyTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var featureDataField = (FeatureDataField)d;
            featureDataField.Refresh();
        }

        /// <summary>
        /// *FOR INTERNAL USE ONLY* : Gets the text box changed listener.
        /// </summary>
        /// <param name="obj"><see cref="DependencyObject"/>.</param>
        /// <exclude/>
        /// <returns><see cref="TextBox"/>.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TextBox GetTextBoxChangedListener(DependencyObject obj)
        {
            return (TextBox)obj.GetValue(TextBoxChangedListenerProperty);
        }

        /// <summary>
        /// *FOR INTERNAL USE ONLY* : Sets the text box changed listener.
        /// </summary>
        /// <param name="obj"><see cref="DependencyObject"/>.</param>
        /// <param name="value"><see cref="TextBox"/>.</param>
        /// <exclude/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetTextBoxChangedListener(DependencyObject obj, TextBox value)
        {
            obj.SetValue(TextBoxChangedListenerProperty, value);
        }

        /// <summary>
        /// *FOR INTERNAL USE ONLY* : The text box changed listener property.
        /// </summary>
        /// <remarks>
        /// Subscribes to TextBox.TextChanged to perform validation while the string content is updated.
        /// If a two-way binding on Text property exists, UpdateSource will push the change to feature attribute,
        /// which will trigger validation based on field type and raise <see cref="ValueChanging"/> to also
        /// accommodate any custom validation.
        /// </remarks>
        /// <exclude/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly DependencyProperty TextBoxChangedListenerProperty =
            DependencyProperty.RegisterAttached("TextBoxChangedListener", typeof(TextBox), typeof(FeatureDataField), new PropertyMetadata(null, OnTextBoxChangedListenerPropertyChanged));

        private static void OnTextBoxChangedListenerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is TextBox)
            {
                var textBox = (TextBox)e.OldValue;
                textBox.TextChanged -= OnTextBoxChanged;
            }

            if (e.NewValue is TextBox)
            {
                var textBox = (TextBox)e.NewValue;
                textBox.TextChanged += OnTextBoxChanged;
            }
        }

        private static void OnTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
            {
                bindingExpression.UpdateSource();
            }
        }

        /// <summary>
        /// Occurs when the field attribute value has changed but has not yet been committed back to the feature.
        /// </summary>
        /// <remarks>
        /// This event may be used to provide custom validation by throwing an exception in the event handler.
        /// </remarks>
        /// <seealso cref="OnValueChanging(object, object)"/>
        public event EventHandler<AttributeValueChangedEventArgs> ValueChanging;

        /// <summary>
        /// Occurs when the field attribute value has changed and committed back to the feature.
        /// </summary>
        /// <seealso cref="OnValueChanged(object, object)"/>
        public event EventHandler<AttributeValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// Generates the control for <see cref="FeatureDataField"/>.
        /// </summary>
        private void Refresh()
        {
            if (DesignTime.IsDesignMode)
            {
                if (_contentControl != null)
                {
                    _contentControl.Content = new ReadOnlyDataItem($"[{FieldName}]", null);
                    _contentControl.ContentTemplate = IsReadOnly ? ReadOnlyTemplate : InputTemplate;
                }

                return;
            }

            // Content control, container for the control, and Field, which provides schema for attribute field,
            // are both required to generate the control.
            if (_contentControl == null || Feature == null)
            {
                return;
            }

            _field = Feature.GetField(FieldName);

            if (_field == null)
            {
                return;
            }

            // Retrieves binding value from feature attribute.
            BindingValue = Feature.Attributes.ContainsKey(FieldName) ? Feature.Attributes[FieldName] : null;

            // Collapses control for unsupported field types and generates appropriate field
            // based on IsReadOnly, IsEditable, Domain.
            switch (_field.FieldType)
            {
                case FieldType.Blob:
                case FieldType.Geometry:
                case FieldType.Raster:
                case FieldType.Xml:
                    {
                        Visibility = Visibility.Collapsed;
                        break;
                    }
            }

            if (IsReadOnly || !_field.IsEditable)
            {
                GenerateReadOnlyField();
            }
            else if (_field.Domain is CodedValueDomain)
            {
                GenerateSelectorField();
            }
            else
            {
                GenerateInputField();
            }
        }

        /// <summary>
        /// Generates data field for <see cref="SelectorTemplate"/>.
        /// </summary>
        private void GenerateSelectorField()
        {
            var cvd = _field.GetCodedValueDomain();
            if (cvd == null)
            {
                return;
            }

            var items = new List<Esri.ArcGISRuntime.Data.CodedValue>();
            if (_field.IsNullable)
            {
                items.Add(default(Esri.ArcGISRuntime.Data.CodedValue));
            }

            items.AddRange(cvd.CodedValues);
            _contentControl.Content = null;
            _contentControl.ContentTemplate = null;
            _dataItem = new SelectorDataItem(ValueChangedCallback, BindingValue, items);
            _contentControl.Content = _dataItem;
            _contentControl.ContentTemplate = SelectorTemplate;
        }

        /// <summary>
        /// Generates data field for <see cref="InputTemplate"/>.
        /// </summary>
        private void GenerateInputField()
        {
            _contentControl.ContentTemplate = InputTemplate;
            _dataItem = new InputDataItem(ValueChangedCallback, BindingValue, _field);
            _contentControl.Content = _dataItem;
        }

        /// <summary>
        /// Generates data field for <see cref="ReadOnlyTemplate"/>.
        /// </summary>
        private void GenerateReadOnlyField()
        {
            _contentControl.ContentTemplate = ReadOnlyTemplate;
            _dataItem = new ReadOnlyDataItem(BindingValue, _field);
            _contentControl.Content = _dataItem;
        }

        /// <summary>
        /// Callback used to track when value changes in one of the <see cref="DataItem"/> classes.
        /// </summary>
        /// <param name="value">new value entered or selected by user.</param>
        private void ValueChangedCallback(object value)
        {
            BindingValue = value;
        }

        /// <summary>
        /// Converts value to its correct field type.
        /// </summary>
        /// <param name="value">value to convert.</param>
        /// <returns>value in its correct field type or value.</returns>
        private object ConvertToFieldType(object value)
        {
            if (_field == null || value == null)
            {
                return null;
            }

            var valueString = value as string;
            if (_field.IsNullable && _field.FieldType != FieldType.Text && valueString?.Trim() == string.Empty)
            {
                return null;
            }
#if NETFX_CORE
            CultureInfo cinfo = new CultureInfo(Language);
#else
            CultureInfo cinfo = Language.GetEquivalentCulture();
#endif
            switch (_field.FieldType)
            {
                case FieldType.Date:
                    {
                        if (value is DateTimeOffset)
                        {
                            return value;
                        }

                        if (string.IsNullOrEmpty(valueString))
                        {
                            return null;
                        }

                        return DateTimeOffset.Parse(valueString, cinfo);
                    }

                case FieldType.Float32:
                    {
                        if (value is float)
                        {
                            return value;
                        }

                        return Convert.ToSingle(valueString, cinfo);
                    }

                case FieldType.Float64:
                    {
                        if (value is double)
                        {
                            return value;
                        }

                        return Convert.ToDouble(valueString, cinfo);
                    }

                case FieldType.Guid:
                    {
                        if (value is Guid)
                        {
                            return value;
                        }

                        if (string.IsNullOrEmpty(valueString))
                        {
                            return null;
                        }

                        return Guid.Parse(valueString);
                    }

                case FieldType.Int16:
                    {
                        if (value is short)
                        {
                            return value;
                        }

                        return Convert.ToInt16(valueString, cinfo);
                    }

                case FieldType.Int32:
                    {
                        if (value is int)
                        {
                            return value;
                        }

                        return Convert.ToInt32(valueString, cinfo);
                    }

                case FieldType.OID:
                    {
                        if (value is long)
                        {
                            return value;
                        }

                        return Convert.ToInt64(valueString, cinfo);
                    }

                case FieldType.Text:
                    {
                        return valueString;
                    }
            }

            return value;
        }

        /// <summary>
        /// Updates the validation state based on <see cref="ValidationException"/>.
        /// </summary>
        private void UpdateValidationState()
        {
            _dataItem.ErrorMessage = ValidationException?.Message ?? string.Empty;
            var deltaState = ValidationException == null ? "ValidState" : (_focused ? "InvalidFocusedState" : "InvalidUnfocusedState");
            if (_currentState != deltaState)
            {
                _currentState = deltaState;
                GoToState(_contentControl, _currentState);
            }
        }

        /// <summary>
        /// Updates visual state of control using visual state manager or visual state group.
        /// </summary>
        /// <param name="element">control or framework element whose state need to change.</param>
        /// <param name="state">target visual state.</param>
        /// <returns>a value indicating whether visual state is changed.</returns>
        private bool GoToState(DependencyObject element, string state)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            if (childCount <= 0)
            {
                return false;
            }

            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                if ((child is Control) && VisualStateManager.GoToState((Control)child, state, true))
                {
                    return true;
                }

                if (child is FrameworkElement && TryValidationState((FrameworkElement)child, state))
                {
                    return true;
                }

                if (GoToState(child, state))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates visual state by invoking storyboard.
        /// </summary>
        /// <param name="element">framework element whose state need to change.</param>
        /// <param name="stateName">validation state whose storyboard need to be invoked.</param>
        /// <returns>a value indicating whether visual state is changed.</returns>
        private bool TryValidationState(FrameworkElement element, string stateName)
        {
            var groups = VisualStateManager.GetVisualStateGroups(element);
            if (groups == null)
            {
                return false;
            }

            bool isStoryboardStarted = false;
            foreach (var state in from VisualStateGroup @group in groups
                                  where @group.Name == "ValidationStates"
                                  from VisualState state in @group.States
                                  where state.Name == stateName
                                  select state)
            {
                isStoryboardStarted = true;
                state.Storyboard.Begin();
            }

            return isStoryboardStarted;
        }

        /// <summary>
        /// Cancels the edit and reverts back to last saved value on feature.
        /// </summary>
        private void Cancel()
        {
            // Field name must match a feature attribute to get its last saved value.
            if (string.IsNullOrEmpty(FieldName)
                || Feature?.Attributes == null
                || !Feature.Attributes.ContainsKey(FieldName))
            {
                return;
            }

            // Clears validation exception
            ClearValue(ValidationExceptionProperty);

            // Override value with last saved value on feature.
            BindingValue = Feature.Attributes[FieldName];
        }

        /// <summary>
        /// Updates feature attribute to commit the attribute edit from <see cref="FeatureDataField"/>.
        /// </summary>
        /// <param name="value">The new attribute value.</param>
        /// <returns>a value indicating whether change is committed.</returns>
        private bool CommitChange(object value)
        {
            try
            {
                Feature.Attributes[FieldName] = ConvertToFieldType((value is KeyValuePair<object, string>) ? ((KeyValuePair<object, string>)value).Key : value);
                ClearValue(ValidationExceptionProperty);
                return true;
            }
            catch (Exception ex)
            {
                SetValue(ValidationExceptionProperty, ex);
                return false;
            }
        }
    }
}
#endif