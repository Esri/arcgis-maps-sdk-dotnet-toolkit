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


using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#elif MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
#endif


#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Displays the list of Attachments in an <see cref="AttachmentsFormElement"/> object.
    /// </summary>
    public partial class AttachmentsFormElementView
    {
        private WeakEventListener<AttachmentsFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;
        private WeakEventListener<AttachmentsFormElementView, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>? _attachmentsCollectionChangedListener;
        private bool _isAttachmentsLoaded;

        /// <summary>
        /// Initializes an instance of the <see cref="AttachmentsFormElementView"/> class.
        /// </summary>
        public AttachmentsFormElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(AttachmentsFormElementView);
#endif
        }

        private void EvaluateExpressions()
        {
            var parent = FeatureFormView.GetFeatureFormViewParent(this);
            _ = parent?.EvaluateExpressions(parent?.FeatureForm);
        }

        /// <summary>
        /// Gets or sets the AttachmentsFormElement.
        /// </summary>
        public AttachmentsFormElement? Element
        {
            get { return (AttachmentsFormElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(AttachmentsFormElement), typeof(AttachmentsFormElementView), null, propertyChanged: (s, oldValue, newValue) => ((AttachmentsFormElementView)s).OnElementPropertyChanged(oldValue as AttachmentsFormElement, newValue as AttachmentsFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(AttachmentsFormElement), typeof(AttachmentsFormElementView), new PropertyMetadata(null, (s, e) => ((AttachmentsFormElementView)s).OnElementPropertyChanged(e.OldValue as AttachmentsFormElement, e.NewValue as AttachmentsFormElement)));
#endif

        private async void OnElementPropertyChanged(AttachmentsFormElement? oldValue, AttachmentsFormElement? newValue)
        {
            _isAttachmentsLoaded = newValue is null;

            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (oldValue?.Attachments is INotifyCollectionChanged oldAttachments)
            {
                _attachmentsCollectionChangedListener?.Detach();
                _attachmentsCollectionChangedListener = null;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<AttachmentsFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Element_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            if (newValue?.Attachments is INotifyCollectionChanged newAttachments)
            {
                _attachmentsCollectionChangedListener = new WeakEventListener<AttachmentsFormElementView, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, newAttachments)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Attachments_CollectionChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
                };
                newAttachments.CollectionChanged += _attachmentsCollectionChangedListener.OnEvent;
            }
            UpdateVisibility();
            UpdateAddAttachmentButtonState();
            UpdateMinMaxAttachmentText();
            if (newValue != null)
            {
                try
                {
                    await newValue.FetchAttachmentsAsync();
                }
                catch (System.Exception) { }
                finally
                {
                    _isAttachmentsLoaded = true;
                    UpdateAddAttachmentButtonState();
                    UpdateMinMaxAttachmentText();
                }
            }
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AttachmentsFormElement.IsVisible))
            {
                this.Dispatch(UpdateVisibility);
            }

            if (e.PropertyName == nameof(AttachmentsFormElement.IsEditable) ||
                e.PropertyName == nameof(AttachmentsFormElement.MaxAttachmentCount) ||
                e.PropertyName == nameof(AttachmentsFormElement.Attachments))
            {
                this.Dispatch(UpdateAddAttachmentButtonState);
            }

            if (e.PropertyName == nameof(AttachmentsFormElement.Attachments))
            {
                if (sender is AttachmentsFormElement element && element.Attachments is INotifyCollectionChanged collection)
                {
                    collection.CollectionChanged -= Attachments_CollectionChanged;
                    collection.CollectionChanged += Attachments_CollectionChanged;
                }

                this.Dispatch(UpdateMinMaxAttachmentText);
            }

            if (e.PropertyName == nameof(AttachmentsFormElement.MinAttachmentCount) ||
                e.PropertyName == nameof(AttachmentsFormElement.MaxAttachmentCount))
            {
                this.Dispatch(UpdateMinMaxAttachmentText);
            }

            if (e.PropertyName == "ValidationErrors")
            {
                this.Dispatch(UpdateMinMaxAttachmentText);
            }
        }

        private void Attachments_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.Dispatch(UpdateAddAttachmentButtonState);
            this.Dispatch(UpdateMinMaxAttachmentText);
        }

        private bool CanAddAttachment()
        {
            if (Element is null || !Element.IsEditable || !_isAttachmentsLoaded)
            {
                return false;
            }

            return Element.MaxAttachmentCount < 0 || Element.Attachments.Count < Element.MaxAttachmentCount;
        }

        private void UpdateVisibility()
        {
#if MAUI
            this.IsVisible = Element?.IsVisible == true;
#else
            this.Visibility = Element?.IsVisible == true ? Visibility.Visible : Visibility.Collapsed;
#endif
        }

        private void UpdateMinMaxAttachmentText()
        {
            if (Element is null)
            {
                UpdateMinMaxAttachmentTextCore(string.Empty, false, string.Empty, false, string.Empty, false);
                return;
            }

            uint minAttachmentCount = Element.MinAttachmentCount;
            uint maxAttachmentCount = Element.MaxAttachmentCount;
            uint attachmentCount = (uint)Element.Attachments.Count;
            var (minErrorFromValidation, maxErrorFromValidation) = GetAttachmentCountValidationErrors(Element.ValidationErrors);
            bool canEvaluateAttachmentCount = _isAttachmentsLoaded;

            bool minVisible = minAttachmentCount > 0;
            bool maxVisible = HasConfiguredMaxAttachmentCount(maxAttachmentCount);
            bool hasMinError = canEvaluateAttachmentCount && minVisible && (minErrorFromValidation || attachmentCount < minAttachmentCount);
            bool hasMaxError = canEvaluateAttachmentCount && maxVisible && (maxErrorFromValidation || attachmentCount > maxAttachmentCount);
            bool hasError = hasMinError || hasMaxError;

            string minText = GetMinimumAttachmentCountLabel(minAttachmentCount);
            string maxText = GetMaximumAttachmentCountLabel(maxAttachmentCount);
            string errorText = GetAttachmentCountErrorMessage(hasMinError, minAttachmentCount, hasMaxError, maxAttachmentCount);

            UpdateMinMaxAttachmentTextCore(minText, minVisible && !hasError, maxText, maxVisible && !hasError, errorText, hasError);
        }

        private partial void UpdateAddAttachmentButtonState();

        private static bool HasConfiguredMaxAttachmentCount(uint maxAttachmentCount)
        {
            return maxAttachmentCount > 0 && maxAttachmentCount < uint.MaxValue;
        }

        private static (bool HasMinimumError, bool HasMaximumError) GetAttachmentCountValidationErrors(IEnumerable<Exception> errors)
        {
            bool hasMinimumError = false;
            bool hasMaximumError = false;

            foreach (var error in errors)
            {
                if (error is FeatureFormLessThanMinimumAttachmentCountException)
                {
                    hasMinimumError = true;
                }
                else if (error is FeatureFormExceedsMaximumAttachmentCountException)
                {
                    hasMaximumError = true;
                }

                if (hasMinimumError && hasMaximumError)
                {
                    break;
                }
            }

            return (hasMinimumError, hasMaximumError);
        }

        private static string GetMinimumAttachmentCountErrorMessage(uint minAttachmentCount)
        {
            return string.Format(Properties.Resources.GetString("FeatureFormMinimumAttachmentCountRequired")!, minAttachmentCount);
        }

        private static string GetMaximumAttachmentCountErrorMessage(uint maxAttachmentCount)
        {
            return string.Format(Properties.Resources.GetString("FeatureFormMaximumAttachmentCountAllowed")!, maxAttachmentCount);
        }

        private static string GetMinimumAttachmentCountLabel(uint minAttachmentCount)
        {
            return string.Format(Properties.Resources.GetString("FeatureFormMinimumAttachmentCountLabel")!, minAttachmentCount);
        }

        private static string GetMaximumAttachmentCountLabel(uint maxAttachmentCount)
        {
            return string.Format(Properties.Resources.GetString("FeatureFormMaximumAttachmentCountLabel")!, maxAttachmentCount);
        }

        private static string GetAttachmentCountErrorMessage(bool hasMinError, uint minAttachmentCount, bool hasMaxError, uint maxAttachmentCount)
        {
            if (hasMinError && hasMaxError)
            {
                return $"{GetMinimumAttachmentCountErrorMessage(minAttachmentCount)} {GetMaximumAttachmentCountErrorMessage(maxAttachmentCount)}";
            }

            if (hasMinError)
            {
                return GetMinimumAttachmentCountErrorMessage(minAttachmentCount);
            }

            if (hasMaxError)
            {
                return GetMaximumAttachmentCountErrorMessage(maxAttachmentCount);
            }

            return string.Empty;
        }

        private bool TryHandleUnsupportedTypeException(Exception ex)
        {
            Exception? current = ex;
            while (current is not null)
            {
                if (current is FeatureFormIncorrectAttachmentTypeException)
                {
                    string allowedTypes = GetAllowedAttachmentTypesForCurrentInputs();
                    string message = string.Format(Properties.Resources.GetString("FeatureFormAttachmentTypeValidationError")!, allowedTypes);
                    _ = ShowAttachmentValidationAlertAsync(message);
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

        private string GetAllowedAttachmentTypesForCurrentInputs()
        {
            var element = Element;
            if (element is null)
            {
                return string.Empty;
            }

            List<string> allowedTypes = new List<string>();
            foreach (var input in element.Inputs)
            {
                if (input is AudioFormInput)
                {
                    AddAllowedType(allowedTypes, "audio");
                }
                else if (input is DocumentFormInput)
                {
                    AddAllowedType(allowedTypes, "document");
                }
                else if (input is ImageFormInput)
                {
                    AddAllowedType(allowedTypes, "image");
                }
                else if (input is VideoFormInput)
                {
                    AddAllowedType(allowedTypes, "video");
                }
            }

            return string.Join(", ", allowedTypes);
        }

        private static void AddAllowedType(List<string> allowedTypes, string allowedType)
        {
            if (!allowedTypes.Contains(allowedType, StringComparer.OrdinalIgnoreCase))
            {
                allowedTypes.Add(allowedType);
            }
        }

        private partial Task ShowAttachmentValidationAlertAsync(string message);

        private partial void UpdateMinMaxAttachmentTextCore(string minAttachmentText, bool minVisible, string maxAttachmentText, bool maxVisible, string errorText, bool errorVisible);
    }
}
