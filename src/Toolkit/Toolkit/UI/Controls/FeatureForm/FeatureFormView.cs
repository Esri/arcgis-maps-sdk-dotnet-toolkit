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
    /// <seealso cref="Esri.ArcGISRuntime.Data.FeatureTable.FeatureFormDefinition"/>
    /// <seealso cref="Esri.ArcGISRuntime.Mapping.FeatureLayer.FeatureFormDefinition"/>
    public partial class FeatureFormView
    {
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
                        _ = await FeatureForm.EvaluateExpressionsAsync();
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
            BindableProperty.Create(nameof(FeatureForm), typeof(FeatureForm), typeof(FeatureFormView), null, propertyChanged: OnFeatureFormPropertyChanged);
#else
        public static readonly DependencyProperty FeatureFormProperty =
            DependencyProperty.Register(nameof(FeatureForm), typeof(FeatureForm), typeof(FeatureFormView),
                new PropertyMetadata(null, (s, e) => FeatureFormView.OnFeatureFormPropertyChanged(s, e.OldValue, e.NewValue)));
#endif

        private static void OnFeatureFormPropertyChanged(DependencyObject d, object oldValue, object newValue)
        {
            var formView = (FeatureFormView)d;
            var oldForm = oldValue as FeatureForm;
            var newForm = newValue as FeatureForm;
            if (newForm is not null)
            {
                formView.InvalidateForm();
            }

#if MAUI
            (formView.GetTemplateChild(FeatureFormContentScrollViewerName) as ScrollViewer)?.ScrollToAsync(0,0,false);
#else
            (formView.GetTemplateChild(FeatureFormContentScrollViewerName) as ScrollViewer)?.ScrollToHome();
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
    }
}
#endif