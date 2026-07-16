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
            if (oldValue?.Attachments is INotifyCollectionChanged oldAttachments)
            {
                oldAttachments.CollectionChanged -= Attachments_CollectionChanged;
            }

            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
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
                newAttachments.CollectionChanged -= Attachments_CollectionChanged;
                newAttachments.CollectionChanged += Attachments_CollectionChanged;
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
            if (Element is null || !Element.IsEditable)
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
            var validationErrors = GetValidationErrors(Element);

            bool minVisible = minAttachmentCount > 0;
            bool maxVisible = HasConfiguredMaxAttachmentCount(maxAttachmentCount);
            bool hasMinError = minVisible && (HasMinimumAttachmentCountError(validationErrors) || attachmentCount < minAttachmentCount);
            bool hasMaxError = maxVisible && (HasMaximumAttachmentCountError(validationErrors) || attachmentCount > maxAttachmentCount);
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

        private static IEnumerable<Exception> GetValidationErrors(AttachmentsFormElement element)
        {
            return element.ValidationErrors;
        }

        private static bool HasMinimumAttachmentCountError(IEnumerable<Exception> errors)
        {
            foreach (var error in errors)
            {
                if (error is FeatureFormLessThanMinimumAttachmentCountException)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMaximumAttachmentCountError(IEnumerable<Exception> errors)
        {
            foreach (var error in errors)
            {
                if (error is FeatureFormExceedsMaximumAttachmentCountException)
                {
                    return true;
                }
            }

            return false;
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

        private partial void UpdateMinMaxAttachmentTextCore(string minAttachmentText, bool minVisible, string maxAttachmentText, bool maxVisible, string errorText, bool errorVisible);
    }
}
