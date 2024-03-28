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

#if WPF || MAUI
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using Esri.ArcGISRuntime.Data;


#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using DependencyObject = Microsoft.Maui.Controls.BindableObject;
using ScrollViewer = Microsoft.Maui.Controls.ScrollView;
#else
using Esri.ArcGISRuntime.Toolkit.Primitives;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// A visual feature editor form controlled by a <see cref="FeatureForm"/> definition.
    /// </summary>
    /// <seealso cref="Esri.ArcGISRuntime.Data.ArcGISFeatureTable.FeatureFormDefinition"/>
    /// <seealso cref="Esri.ArcGISRuntime.Mapping.FeatureLayer.FeatureFormDefinition"/>
    public partial class FeatureFormView
    {
        private WeakEventListener<FeatureFormView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureFormView"/> class.
        /// </summary>
        public FeatureFormView()
            : base()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(FeatureFormView);
#endif
        }
        /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            InvalidateForm();
        }

        private bool _isDirty = false;
        private object _isDirtyLock = new object();

#if MAUI
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm.Elements), "Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm", "Esri.ArcGISRuntime")]
#endif
        private void InvalidateForm()
        {
            lock (_isDirtyLock)
            {
                if (_isDirty)
                {
                    return;
                }
                _isDirty = true;
            }
#if MAUI
            Dispatcher.Dispatch(async () =>
#else
            _ = Dispatcher.InvokeAsync(async () =>
#endif
            {
                try
                {
                    lock (_isDirtyLock)
                    {
                        _isDirty = false;
                    }
                    if (FeatureForm != null)
                    {
                        await EvaluateExpressions(FeatureForm);
#if MAUI
                        var ctrl = GetTemplateChild(ItemsViewName) as IBindableLayout;
                        if (ctrl != null && ctrl is BindableObject bo)
                        {
                            bo.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("FeatureForm.Elements", source: RelativeBindingSource.TemplatedParent)); // TODO: Should update binding instead
                        }
#else
                        var ctrl = GetTemplateChild(ItemsViewName) as ItemsControl;
                        var binding = ctrl?.GetBindingExpression(ItemsControl.ItemsSourceProperty);
                        binding?.UpdateTarget();
#endif
                    }
                }
                catch
                {
                }
            });
        }

        private static object pendingExpressionsLock = new object();
        private static List<FeatureForm> pendingExpressions = new List<FeatureForm>();

        internal static async Task EvaluateExpressions(FeatureForm? form)
        {
            if (form is null)
                return;
            // Don't evaluate expressions if we're already in the process of evaluating
            // If that's the case, the value changed event triggering this code was
            // caused by another expression evaluation
            lock (pendingExpressionsLock)
            {
                if (pendingExpressions.Contains(form))
                    return;
                pendingExpressions.Add(form);
            }
            try
            {
                await form.EvaluateExpressionsAsync();
            }
            catch { }
            finally
            {
                lock (pendingExpressionsLock)
                {
                    if (pendingExpressions.Contains(form))
                        pendingExpressions.Remove(form);
                }
            }
        }

        /// <summary>
        /// Gets or sets the associated FeatureForm which contains the form.
        /// </summary>
        public FeatureForm? FeatureForm
        {
            get { return GetValue(FeatureFormProperty) as FeatureForm; }
            set { SetValue(FeatureFormProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FeatureForm"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty FeatureFormProperty =
            BindableProperty.Create(nameof(FeatureForm), typeof(FeatureForm), typeof(FeatureFormView), null, propertyChanged: (s, oldValue, newValue) => ((FeatureFormView)s).OnFeatureFormPropertyChanged(oldValue, newValue));
#else
        public static readonly DependencyProperty FeatureFormProperty =
            DependencyProperty.Register(nameof(FeatureForm), typeof(FeatureForm), typeof(FeatureFormView),
                new PropertyMetadata(null, (s, e) => ((FeatureFormView)s).OnFeatureFormPropertyChanged(e.OldValue, e.NewValue)));
#endif

        private void OnFeatureFormPropertyChanged(object oldValue, object newValue)
        {
            var oldForm = oldValue as FeatureForm;
            var newForm = newValue as FeatureForm;
            if (newForm is not null)
            {
                InvalidateForm();
            }

#if MAUI
            (GetTemplateChild(FeatureFormContentScrollViewerName) as ScrollViewer)?.ScrollToAsync(0,0,false);
#else
            (GetTemplateChild(FeatureFormContentScrollViewerName) as ScrollViewer)?.ScrollToHome();
#endif

            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<FeatureFormView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.FeatureForm_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            UpdateIsValidProperty();
        }

        private void FeatureForm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FeatureForm.ValidationErrors))
            {
                Dispatch(UpdateIsValidProperty);
            }
        }

        private void UpdateIsValidProperty()
        {
            IsValid = FeatureForm?.ValidationErrors?.Any() != true;
        }

        private void Dispatch(Action action)
        {
#if WPF
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.Invoke(action);
#elif MAUI
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(action);
            else
                action();
#endif
        }

        /// <summary>
        /// Gets or sets the vertical scrollbar visibility of the scrollviewer below the title.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VerticalScrollBarVisibility"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty VerticalScrollBarVisibilityProperty =
            BindableProperty.Create(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(FeatureFormView), ScrollBarVisibility.Default);
#else
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(FeatureFormView), new PropertyMetadata(ScrollBarVisibility.Auto));
#endif

        /// <summary>
        /// Localizes a specific FeatureForm error message and adding contextual type/range/domain information to the error message.
        /// This error message should be displayed to the user.
        /// </summary>
        /// <param name="element">Field Form Element the error is thrown for, to add type/range/domain context to the error message.</param>
        /// <param name="exception">The returned validation exception.</param>
        /// <returns>Localized string for the error message.</returns>
        internal static string? ValidationErrorToLocalizedString(FieldFormElement element, Exception exception)
        {
            if (exception is FeatureFormNullNotAllowedException)
            {
                return Properties.Resources.GetString("FeatureFormNullNotAllowed");
            }
            else if (exception is FeatureFormIncorrectValueTypeException)
            {
                if (element.FieldType == FieldType.Int16 || element.FieldType == FieldType.Int32)
                    return Properties.Resources.GetString("FeatureFormIncorrectValueTypeNumeric");
                if (element.FieldType == FieldType.Float32 || element.FieldType == FieldType.Float64)
                    return Properties.Resources.GetString("FeatureFormIncorrectValueTypeFloatingPoint");
                return Properties.Resources.GetString("FeatureFormIncorrectValueType");
            }
            else if (exception is FeatureFormExceedsMaximumDateTimeException)
            {
                if (element.Domain is RangeDomain<DateTime> range && element.Input is DateTimePickerFormInput dateinput)
                {
                    var formatString = Properties.Resources.GetString("FeatureFormExceedsMaximumDateTime");
                    return string.Format(formatString!, dateinput.IncludeTime ? range.MaxValue.ToString() : range.MaxValue.Date.ToShortDateString());
                }
                return Properties.Resources.GetString("FeatureFormExceedsMaximumDateTimeNoRange");
            }
            else if (exception is FeatureFormLessThanMinimumDateTimeException)
            {
                if (element.Domain is RangeDomain<DateTime> range && element.Input is DateTimePickerFormInput dateinput)
                {
                    var formatString = Properties.Resources.GetString("FeatureFormLessThanMinimumDateTime");
                    return string.Format(formatString!, dateinput.IncludeTime ? range.MinValue.ToString() : range.MinValue.Date.ToShortDateString());
                }
                return Properties.Resources.GetString("FeatureFormLessThanMinimumDateTimeNoRange");
            }
            else if (exception is FeatureFormExceedsMaximumLengthException || exception is FeatureFormLessThanMinimumLengthException)
            {
                uint max = 0;
                uint min = 0;
                if (element.Input is TextAreaFormInput area) { max = area.MaxLength; min = area.MinLength; }
                else if (element.Input is TextBoxFormInput tb) { max = tb.MaxLength; min = tb.MinLength; }
                if (max > 0 && min > 0)
                    return string.Format(Properties.Resources.GetString("FeatureFormOutsideLengthRange")!, min, max);
                if (max > 0)
                    return string.Format(Properties.Resources.GetString("FeatureFormExceedsMaximumLength")!, max);
                if (exception is FeatureFormExceedsMaximumLengthException)
                    return Properties.Resources.GetString("FeatureFormExceedsMaximumLengthNoRange");
                if (min > 0)
                    return string.Format(Properties.Resources.GetString("FeatureFormLessThanMinimumLength")!, max);
                return Properties.Resources.GetString("FeatureFormExceedsMaximumLengthNoRange");
            }
            else if (exception is FeatureFormExceedsNumericMaximumException || exception is FeatureFormLessThanNumericMinimumException)
            {
                double max = double.NaN;
                double min = double.NaN;
                if (element.Domain is RangeDomain<int> intrange)
                {
                    min = intrange.MinValue;
                    max = intrange.MaxValue;
                }
                else if (element.Domain is RangeDomain<short> shortrange)
                {
                    min = shortrange.MinValue;
                    max = shortrange.MaxValue;
                }
                else if (element.Domain is RangeDomain<float> floatrange)
                {
                    min = floatrange.MinValue;
                    max = floatrange.MaxValue;
                }
                else if (element.Domain is RangeDomain<double> doublerange)
                {
                    min = doublerange.MinValue;
                    max = doublerange.MaxValue;
                }
                else if (element.Domain is RangeDomain<long> longrange)
                {
                    min = longrange.MinValue;
                    max = longrange.MaxValue;
                }
                if (!double.IsNaN(max) && !double.IsNaN(min))
                    return string.Format(Properties.Resources.GetString("FeatureFormExceedsNumericOutsideRange")!, min, max);
                if (!double.IsNaN(max))
                    return string.Format(Properties.Resources.GetString("FeatureFormExceedsNumericMaximum")!, max);
                if (exception is FeatureFormExceedsNumericMaximumException)
                    return Properties.Resources.GetString("FeatureFormExceedsMaximumLengthNoRange");
                return Properties.Resources.GetString("FeatureFormLessThanNumericMinimum");
            }
            else if (exception is FeatureFormNotInCodedValueDomainException)
                return Properties.Resources.GetString("FeatureFormNotInCodedValueDomain");
            else if (exception is FeatureFormFieldIsRequiredException)
                return Properties.Resources.GetString("FeatureFormFieldIsRequired");
            return exception.Message;
        }
    }
}
#endif