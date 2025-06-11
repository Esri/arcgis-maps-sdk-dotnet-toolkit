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

#if WPF || WINDOWS_XAML
using System.Diagnostics;
using Esri.ArcGISRuntime.Mapping.FeatureForms;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.FeatureFormView"/> control,
    /// used for rendering a set of popup elements responsible for rendering <see cref="FieldFormElement"/> and <see cref="GroupFormElement"/>.
    /// </summary>
    public partial class FormElementItemsControl : ItemsControl
    {
        private static DataTemplate UnsupportedFormElementTemplate = new DataTemplate();

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is ContentPresenter presenter)
            {
                if (item is FieldFormElement)
                {
                    presenter.ContentTemplate = FieldFormElementTemplate;
                }
                else if (item is TextFormElement)
                {
                    presenter.ContentTemplate = TextFormElementTemplate;
                }
                else if (item is GroupFormElement)
                {
                    presenter.ContentTemplate = GroupFormElementTemplate;
                }
                else if (item is UtilityAssociationsFormElement)
                {
                    presenter.ContentTemplate = UtilityAssociationsFormElementTemplate;
                }
                else
                {
                    presenter.ContentTemplate = UnsupportedFormElementTemplate; // Renders empty / skips
                }
            }
        }

        /// <summary>
        /// Template used for rendering a <see cref="FieldFormElement"/>.
        /// </summary>
        /// <seealso cref="FieldFormElement"/>
        /// <seealso cref="FieldFormElementView"/>
        public DataTemplate FieldFormElementTemplate
        {
            get { return (DataTemplate)GetValue(FieldFormElementTemplateProperty); }
            set { SetValue(FieldFormElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FieldFormElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FieldFormElementTemplateProperty =
            DependencyProperty.Register(nameof(FieldFormElementTemplate), typeof(DataTemplate), typeof(FormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="TextFormElement"/>.
        /// </summary>
        /// <seealso cref="TextFormElement"/>
        /// <seealso cref="TextFormElementView"/>
        public DataTemplate TextFormElementTemplate
        {
            get { return (DataTemplate)GetValue(TextFormElementTemplateProperty); }
            set { SetValue(TextFormElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TextFormElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextFormElementTemplateProperty =
            DependencyProperty.Register(nameof(TextFormElementTemplate), typeof(DataTemplate), typeof(FormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="GroupFormElement"/>.
        /// </summary>
        /// <seealso cref="GroupFormElement"/>
        public DataTemplate GroupFormElementTemplate
        {
            get { return (DataTemplate)GetValue(GroupFormElementTemplateProperty); }
            set { SetValue(GroupFormElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GroupFormElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GroupFormElementTemplateProperty =
            DependencyProperty.Register(nameof(GroupFormElementTemplate), typeof(DataTemplate), typeof(FormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="GroupFormElement"/>.
        /// </summary>
        /// <seealso cref="GroupFormElement"/>
        public DataTemplate UtilityAssociationsFormElementTemplate
        {
            get { return (DataTemplate)GetValue(UtilityAssociationsFormElementTemplateProperty); }
            set { SetValue(UtilityAssociationsFormElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="UtilityAssociationsFormElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UtilityAssociationsFormElementTemplateProperty =
            DependencyProperty.Register(nameof(UtilityAssociationsFormElementTemplate), typeof(DataTemplate), typeof(FormElementItemsControl), new PropertyMetadata(null));
    }
}
#endif