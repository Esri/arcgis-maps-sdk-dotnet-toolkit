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
            this.Dispatch(async () =>
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
                            bo.SetBinding(BindableLayout.ItemsSourceProperty, static (FeatureForm form) => form.Elements, source: RelativeBindingSource.TemplatedParent);
                        }
#elif WPF
                        var ctrl = GetTemplateChild(ItemsViewName) as ItemsControl;
                        var binding = ctrl?.GetBindingExpression(ItemsControl.ItemsSourceProperty);
                        binding?.UpdateTarget();
#elif WINDOWS_XAML
                        var ctrl = GetTemplateChild(ItemsViewName) as ItemsControl;
                        ctrl?.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("FeatureForm.Elements"), Source = this });
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
        private bool _isDiscarding = false;

        internal async Task EvaluateExpressions(FeatureForm? form)
        {
            if (form is null || _isDiscarding)
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
            BindableProperty.Create(nameof(FeatureForm), typeof(FeatureForm), typeof(FeatureFormView), null, propertyChanged: (s, oldValue, newValue) => ((FeatureFormView)s).OnFeatureFormPropertyChanged(oldValue as FeatureForm, newValue as FeatureForm));
#else
        public static readonly DependencyProperty FeatureFormProperty =
            DependencyProperty.Register(nameof(FeatureForm), typeof(FeatureForm), typeof(FeatureFormView),
                new PropertyMetadata(null, (s, e) => ((FeatureFormView)s).OnFeatureFormPropertyChanged(e.OldValue as FeatureForm, e.NewValue as FeatureForm)));
#endif

        private void OnFeatureFormPropertyChanged(FeatureForm? oldForm, FeatureForm? newForm)
        {
            if (newForm is not null)
            {
                InvalidateForm();
            }

#if MAUI
            (GetTemplateChild(FeatureFormContentScrollViewerName) as ScrollViewer)?.ScrollToAsync(0,0,false);
#elif WPF
            (GetTemplateChild(FeatureFormContentScrollViewerName) as ScrollViewer)?.ScrollToHome();
#elif WINDOWS_XAML
            (GetTemplateChild(FeatureFormContentScrollViewerName) as ScrollViewer)?.ChangeView(null, 0, null, disableAnimation: true);
#endif

            if (oldForm is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (newForm is INotifyPropertyChanged inpcNew)
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
                this.Dispatch(UpdateIsValidProperty);
            }
        }

#if !WPF
        private bool _isValid = false;

        /// <summary>
        /// Gets a value indicating whether this form has any validation errors.
        /// </summary>
        /// <seealso cref="FeatureForm.ValidationErrors"/>
        public bool IsValid
        {
            get => _isValid;
            private set
            {
                if (_isValid != value)
                {
                    _isValid = value;
#if MAUI
                    base.OnPropertyChanged(nameof(IsValid));
#endif
                }
            }
        }
#endif

        private void UpdateIsValidProperty()
        {
            IsValid = FeatureForm?.ValidationErrors?.Any() != true;
        }

        /// <summary>
        /// Discards edits to all elements, refreshes any pending expressions and restores attachment list.
        /// </summary>
        /// <seealso cref="FeatureForm.DiscardEdits"/>
        /// <seealso cref="FeatureForm.EvaluateExpressionsAsync"/>
        public async Task DiscardEditsAsync()
        {
            var form = FeatureForm;
            if (form is not null)
            {
                _isDiscarding = true;
                form.DiscardEdits();
                _isDiscarding = false;
                if (form.DefaultAttachmentsElement is not null)
                    await form.DefaultAttachmentsElement.FetchAttachmentsAsync();
                await EvaluateExpressions(form).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///  Saves edits made using the <see cref="FeatureForm"/> to the database.
        /// </summary>
        /// <seealso cref="FeatureForm.FinishEditingAsync"/>
        public async Task FinishEditingAsync()
        {
            if (FeatureForm is not null)
            {
                await FeatureForm.FinishEditingAsync().ConfigureAwait(false);
            }
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
                else if (element.Input is BarcodeScannerFormInput bar) { max = bar.MaxLength; min = bar.MinLength; }
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

        /// <summary>
        /// Raised when a feature form attachment is clicked
        /// </summary>
        /// <remarks>
        /// <para>By default, when an attachment is clicked, the default application for the file type (if any) is launched. To override this,
        /// listen to this event, set the <see cref="FormAttachmentClickedEventArgs.Handled"/> property to <c>true</c> and perform
        /// your own logic. </para>
        /// <example>
        /// Example: Use the .NET MAUI share API for the attachment:
        /// <code language="csharp">
        /// private async void FormAttachmentClicked(object sender, FormAttachmentClickedEventArgs e)
        /// {
        ///     e.Handled = true; // Prevent default launch action
        ///     await Share.Default.RequestAsync(new ShareFileRequest(new ReadOnlyFile(e.FilePath!, e.ContentType)));
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        public event EventHandler<FormAttachmentClickedEventArgs>? FormAttachmentClicked;

        internal bool OnFormAttachmentClicked(FormAttachment attachment)
        {
            var handler = FormAttachmentClicked;
            if (handler is not null)
            {
                var args = new FormAttachmentClickedEventArgs(attachment);
                FormAttachmentClicked?.Invoke(this, args);
                return args.Handled;
            }
            return false;
        }

        /// <summary>
        /// Raised when the Barcode Icon in a barcode element is clicked.
        /// </summary>
        /// <remarks>
        /// <para>Set the <see cref="BarcodeButtonClickedEventArgs.Handled"/> property to <c>true</c> to prevent
        /// any default code and perform your own logic.</para>
        /// </remarks>
        public event EventHandler<BarcodeButtonClickedEventArgs>? BarcodeButtonClicked;

        internal bool OnBarcodeButtonClicked(FieldFormElement element)
        {
            var handler = BarcodeButtonClicked;
            if (handler is not null)
            {
                var args = new BarcodeButtonClickedEventArgs(element);
                
                handler.Invoke(this, args);
                return args.Handled;
            }
            return false;
        }

        // Called when clicking links in markdown
        internal void OnHyperlinkClicked(Uri uri)
        {
            Launcher.LaunchUriAsync(uri);
        }
    }

    /// <summary>
    /// Event argument for the <see cref="FeatureFormView.FormAttachmentClicked"/> event.
    /// </summary>
    public class FormAttachmentClickedEventArgs : EventArgs
    {
        internal FormAttachmentClickedEventArgs(FormAttachment attachment)
        {
            Attachment = attachment;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event handler has handled the event and the default action should be prevented.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the attachment that was clicked.
        /// </summary>
        public FormAttachment Attachment { get; }
    }

    /// <summary>
    /// Event argument for the <see cref="FeatureFormView.BarcodeButtonClicked"/> event.
    /// </summary>
    public class BarcodeButtonClickedEventArgs : EventArgs
    {
        internal BarcodeButtonClickedEventArgs(FieldFormElement element)
        {
            FormElement = element;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event handler has handled the event and the default action should be prevented.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the element that was clicked.
        /// </summary>
        public FieldFormElement FormElement { get; }
    }
}
