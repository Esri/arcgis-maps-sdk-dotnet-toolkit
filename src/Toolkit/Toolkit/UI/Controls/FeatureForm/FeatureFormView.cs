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

            DiscardEditsCommand = new Command(() => _ = DiscardEditsAsync(), () => CurrentFeatureForm?.HasEdits == true);
            FinishEditingCommand = new Command(async () =>
            {
                try
                {
                    await FinishEditingAsync(true);
                }
                catch (System.Exception ex)
                {
#if WPF
                    ShowError(Properties.Resources.GetString("FeatureFormApplyEditsErrorTitle")!, ex.Message);
#else
                    await ShowErrorAsync(Properties.Resources.GetString("FeatureFormApplyEditsErrorTitle")!, ex.Message);
#endif
                }
            }, () => CurrentFeatureForm?.HasEdits == true);
        }

        private class Command : System.Windows.Input.ICommand
        {
            private Action _execute;
            private Func<bool> _canExecute;

            public Command(Action execute, Func<bool> canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            internal void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute();

            public void Execute(object? parameter) => _execute();
        }

        /// <summary>
        /// Command for calling <see cref="FinishEditingAsync(bool)"/> and applying edits to the currently active Feature Form if all fields are valid.
        /// </summary>
        /// <seealso cref="FinishEditingAsync(bool)"/>
        /// <seealso cref="IsValid"/>
        /// <seealso cref="FeatureForm.HasEdits"/>
        /// <seealso cref="DiscardEditsCommand"/>
        public System.Windows.Input.ICommand FinishEditingCommand { get; }

        /// <summary>
        /// Discards any pending edits to the currently active Feature Form, if any edits have been made
        /// </summary>
        /// <seealso cref="DiscardEditsAsync"/>
        /// <seealso cref="FeatureForm.HasEdits"/>
        public System.Windows.Input.ICommand DiscardEditsCommand { get; }

        /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            InvalidateForm();
            if (GetTemplateChild("SubFrameView") is NavigationSubView subView)
            {
                subView.OnNavigating += SubView_OnNavigating;
                _ = subView.Navigate(content: FeatureForm, true);
            }
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
                    if (CurrentFeatureForm != null)
                    {
                        await EvaluateExpressions(CurrentFeatureForm);
                    }
                }
                catch
                {
                }
            });
        }

        /// <summary>
        /// Gets or sets the associated PopupManager which contains popup and sketch editor.
        /// </summary>
        public ValidationErrorVisibility ErrorsVisibility
        {
            get { return (ValidationErrorVisibility)GetValue(ErrorsVisibilityProperty); }
            set { SetValue(ErrorsVisibilityProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ErrorsVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorsVisibilityProperty =
            PropertyHelper.CreateProperty<ValidationErrorVisibility, FeatureFormView>(nameof(ErrorsVisibility), ValidationErrorVisibility.Visible, (s, oldValue, newValue) => s.OnErrorsVisibilityChanged(oldValue, newValue));

        private void OnErrorsVisibilityChanged(ValidationErrorVisibility oldValue, ValidationErrorVisibility newValue)
        {
            foreach (var item in GetDescendentsOfType<FieldFormElementView>(this))
            {
                item.ResetValidationState();
                ((Command)FinishEditingCommand).RaiseCanExecuteChanged();
            }
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
        /// <seealso cref="CurrentFeatureForm"/>
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
            if (GetTemplateChild("SubFrameView") is NavigationSubView subView)
            {
                _ = subView.Navigate(content: newForm, true);
            }
        }

        private void FeatureForm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FeatureForm.ValidationErrors))
            {
                this.Dispatch(UpdateIsValidProperty);
            }
            else if (e.PropertyName == nameof(FeatureForm.HasEdits))
            {
                this.Dispatch(() =>
                {
                    ((Command)FinishEditingCommand).RaiseCanExecuteChanged();
                    ((Command)DiscardEditsCommand).RaiseCanExecuteChanged();
                });
            }
        }

#if !WPF
        private bool _isValid = false;

        /// <summary>
        /// Gets a value indicating whether the <see cref="CurrentFeatureForm"/> has any validation errors.
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
                    ((Command)FinishEditingCommand).RaiseCanExecuteChanged();
                }
            }
        }
#endif

        private void UpdateIsValidProperty()
        {
            IsValid = CurrentFeatureForm?.ValidationErrors?.Any() != true;
        }

        /// <summary>
        /// Discards edits to all elements, refreshes any pending expressions and restores attachment list for the <see cref="CurrentFeatureForm"/> .
        /// </summary>
        /// <seealso cref="FeatureForm.DiscardEdits"/>
        /// <seealso cref="FeatureForm.EvaluateExpressionsAsync"/>
        public async Task DiscardEditsAsync()
        {
            var form = CurrentFeatureForm;
            if (form is not null)
            {
                _isDiscarding = true;
                _wasFinishEditingAttempted = false;
                form.DiscardEdits();
                _isDiscarding = false;
                if (form.DefaultAttachmentsElement is not null)
                    await form.DefaultAttachmentsElement.FetchAttachmentsAsync();
                await EvaluateExpressions(form).ConfigureAwait(false);
                Esri.ArcGISRuntime.Toolkit.Internal.DispatcherExtensions.Dispatch(this, ResetValidationStates);
            }
        }

        private void ResetValidationStates()
        {
            foreach (var item in GetDescendentsOfType<FieldFormElementView>(this))
            {
                item.ResetValidationState();
            }
        }

        private bool _wasFinishEditingAttempted = false;

        internal bool ShouldShowError()
        {
            return ErrorsVisibility == ValidationErrorVisibility.Visible || _wasFinishEditingAttempted;
        }

        private IEnumerable<FieldFormElement> EnumerateVisibleElements(IEnumerable<FormElement>? elements)
        {
            if (elements is not null)
            {
                foreach (var element in elements)
                {
                    if (element.IsVisible)
                    {
                        if (element is FieldFormElement field) yield return field;
                        else if (element is GroupFormElement group)
                        {
                            foreach (var elm in EnumerateVisibleElements(group.Elements))
                                yield return elm;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Scrolls to the first element with a visible validation error
        /// </summary>
        /// <returns><c>True</c> if a form element has an error it could scroll to, otherwise <c>false</c>.</returns>
        public bool ScrollToFirstError()
        {
            foreach (var item in EnumerateVisibleElements(CurrentFeatureForm?.Elements))
            {
                bool elementHasVisibleError = item.ValidationErrors.Any() && item.IsEditable == true;
                if (elementHasVisibleError)
                {
                    ScrollTo(item);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Scrolls to the specified form element
        /// </summary>
        /// <param name="element">Form element to scrollto.</param>
        public void ScrollTo(FormElement element)
        {
            foreach (var item in GetDescendentsOfType<FieldFormElementView>(this))
            {
                if (item.Element == element)
                {
#if WINDOWS_XAML
                    item.StartBringIntoView();
#elif WPF
                    item.BringIntoView();
#elif MAUI
                    if (GetTemplateChild("SubFrameView") is NavigationSubView subView)
                    {
                        _ = subView.ScrollToAsync(item, ScrollToPosition.MakeVisible, true);
                    }
#endif
                }
            }
        }

        /// <summary>
        ///  Saves edits made using the <see cref="FeatureForm"/> to the database for the <see cref="CurrentFeatureForm"/>.
        /// </summary>
        /// <remarks>
        /// Use this method to perform your own validation logic, or if you want to decide which errors are important
        /// prior to applying edits. Alternatively you can use the <see cref="FinishEditingCommand"/>
        /// which will handle showing validation errors and scroll to them.
        /// </remarks>
        /// <seealso cref="FinishEditingAsync(bool)"/>
        /// <seealso cref="FinishEditingCommand"/>
        /// <seealso cref="ErrorsVisibility"/>
        /// <seealso cref="ScrollToFirstError()"/>
        public async Task FinishEditingAsync()
        {
            if (CurrentFeatureForm is not null)
            {
                await CurrentFeatureForm.FinishEditingAsync().ConfigureAwait(false);
                _wasFinishEditingAttempted = false;
                Esri.ArcGISRuntime.Toolkit.Internal.DispatcherExtensions.Dispatch(this, ResetValidationStates);
            }
        }

        /// <summary>
        ///  Saves edits made using the <see cref="FeatureForm"/> to the database for the <see cref="CurrentFeatureForm"/> if there are not errors,
        ///  otherwise scroll to the first error if <paramref name="requireAllErrorsResolved"/> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Use this method to perform your own validation logic, or if you want to decide which errors are important
        /// prior to applying edits. Alternatively you can use the <see cref="FinishEditingCommand"/>
        /// which will handle showing validation errors and scroll to them.
        /// </remarks>
        /// <seealso cref="FinishEditingAsync()"/>
        /// <seealso cref="FinishEditingCommand"/>
        /// <seealso cref="ErrorsVisibility"/>
        /// <seealso cref="ScrollToFirstError()"/>
        public async Task<bool> FinishEditingAsync(bool requireAllErrorsResolved)
        {
            if (CurrentFeatureForm is not null)
            {
                _wasFinishEditingAttempted = true;
                foreach (var item in GetDescendentsOfType<FieldFormElementView>(this))
                {
                    item.ResetValidationState();
                }
               ((Command)FinishEditingCommand).RaiseCanExecuteChanged();
                if (requireAllErrorsResolved && ScrollToFirstError()) return false;
                await FinishEditingAsync();
                return true;
            }
            return false;
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




        private async void SubView_OnNavigating(object? sender, NavigationSubView.NavigationEventArgs e)
        {
            if (CurrentFeatureForm?.HasEdits == true &&
                (e.Direction == NavigationSubView.NavigationDirection.Forward && e.NavigatingTo is FeatureForm ||
                e.Direction == NavigationSubView.NavigationDirection.Backward && e.NavigatingFrom is FeatureForm))
            {
                // If the current feature form has edits, we need to discard or save them before navigating to a new form.
                string title = Properties.Resources.GetString("FeatureFormPendingEditsTitle")!;
                string content = Properties.Resources.GetString("FeatureFormPendingEditsMessage")!;
                string applyText = Properties.Resources.GetString("FeatureFormPendingEditsApply")!;
                string discardText = Properties.Resources.GetString("FeatureFormPendingEditsDiscard")!;
                string cancelText = Properties.Resources.GetString("FeatureFormPendingEditsCancel")!;
#if WINDOWS_XAML
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    PrimaryButtonText = applyText,
                    SecondaryButtonText = discardText,
                    CloseButtonText = cancelText
                };
                dialog.XamlRoot = this.XamlRoot;
                var deferral = e.GetDeferral();
                try
                {
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        await FinishEditingAsync();
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        CurrentFeatureForm?.DiscardEdits();
                    }
                    else if (result == ContentDialogResult.None)
                    {
                        e.Cancel = true;
                    }
                }
                catch(System.Exception ex)
                {
                    await ShowErrorAsync(Properties.Resources.GetString("FeatureFormApplyEditsErrorTitle")!, ex.Message);
                    e.Cancel = true;
                }
                finally
                {
                    deferral.Complete();
                }
#elif WPF
                var result = MessageBox.Show(content, title, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    try
                    {
                        await FinishEditingAsync();
                    }
                    catch(System.Exception ex)
                    {
                        e.Cancel = true;
                        ShowError(Properties.Resources.GetString("FeatureFormApplyEditsErrorTitle")!, ex.Message);
                    }
                }
                else
                {
                    e.Cancel = true;
                }
#elif MAUI
                var page = GetParent<Page>(this);
                if (page is null)
                    e.Cancel = true;
                else
                {
                    var deferral = e.GetDeferral();
                    try
                    {
                        string action = await page.DisplayActionSheet(title, cancelText, null, applyText, discardText);
                        if (action == applyText)
                        {
                            await FinishEditingAsync();
                        }
                        else if (action == discardText)
                        {
                            CurrentFeatureForm?.DiscardEdits();
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                    catch(System.Exception ex)
                    {
                        await ShowErrorAsync(Properties.Resources.GetString("FeatureFormApplyEditsErrorTitle")!, ex.Message);
                        e.Cancel = true;
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                }
#endif
            }

            if (e.Cancel)
                return;

            if (e.NavigatingTo is FeatureForm toff)
            {
                SetCurrentFeatureForm(toff);
            }
            else if (e.NavigatingFrom is FeatureForm fromff && e.Direction == NavigationSubView.NavigationDirection.Backward)
            {
                // 
                var previousForm = ((NavigationSubView?)sender)?.NavigationStack.OfType<FeatureForm>().Where(o => o != fromff)?.FirstOrDefault();
                if (previousForm is not null)
                    SetCurrentFeatureForm(previousForm);
            }
        }


#if WPF
        private void ShowError(string title, string content)
#else
        private async Task ShowErrorAsync(string title, string content)
#endif
        {
#if MAUI
            var page = GetParent<Page>(this);
            if(page is not null) {
                await page.DisplayAlert(title, content, Properties.Resources.GetString("FeatureFormPendingEditsCancel")!);
            }
#elif WINDOWS_XAML
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = Properties.Resources.GetString("FeatureFormPendingEditsCancel")!
            };
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
#elif WPF
            MessageBox.Show(content, title);
#endif
            }

        internal void NavigateToItem(object item)
        {
            if (GetTemplateChild("SubFrameView") is NavigationSubView subView)
            {
                _ = subView.Navigate(content: item);
            }
        }

        /// <summary>
        /// Gets the currently active feature form being edited
        /// </summary>
        /// <remarks>
        /// If you are editing related features or utility network associations, this will return the currently active featureform being edited.
        /// </remarks>
        public FeatureForm? CurrentFeatureForm
        {
            get => GetCurrentFeatureForm();
        }

        private void OnCurrentFeatureFormPropertyChanged(FeatureForm? oldForm, FeatureForm? newForm)
        {
            _wasFinishEditingAttempted = false;

            if (newForm is not null)
            {
                InvalidateForm();
            }

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
            ((Command)FinishEditingCommand).RaiseCanExecuteChanged();
            ((Command)DiscardEditsCommand).RaiseCanExecuteChanged();
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

    /// <summary>
    /// Defines when validation errors should be shown in the <see cref="FeatureFormView"/>.
    /// </summary>
    /// <seealso cref="FeatureFormView.ErrorsVisibility"/>
    public enum ValidationErrorVisibility
    {
        /// <summary>
        /// All errors are visible for every editable element.
        /// </summary>
        Visible,

        /// <summary>
        /// All errors are hidden by default and made visible once an element has received interaction or the user attempts to finish editing.
        /// </summary>
        Automatic,
    }
}
