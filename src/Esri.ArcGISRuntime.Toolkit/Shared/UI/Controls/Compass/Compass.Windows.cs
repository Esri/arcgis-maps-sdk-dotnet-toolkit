// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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
using System.ComponentModel;
using System.Windows;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#else
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class Compass
    {
        private bool _isVisible;

        private void Initialize() => DefaultStyleKey = typeof(Compass);

        /// <inheritdoc/>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            _isVisible = false;
            UpdateCompassRotation(false);
        }

        /// <summary>
        /// Gets or sets the platform-specific implementation of the <see cref="HeadingProperty"/> property
        /// </summary>
        private double HeadingImpl
        {
            get { return (double)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Heading"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register(nameof(Heading), typeof(double), typeof(Compass), new PropertyMetadata(0d, OnHeadingPropertyChanged));
        
        /// <summary>
        /// The property changed event that is raised when
        /// the value of Scale property changes.
        /// </summary>
        /// <param name="d">ScaleLine</param>
        /// <param name="e">Contains information related to the change to the Scale property.</param>
        private static void OnHeadingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = (Compass)d;
            compass.UpdateCompassRotation(true);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0
        /// </summary>
        private bool AutoHideImpl
        {
            get { return (bool)GetValue(AutoHideProperty); }
            set { SetValue(AutoHideProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoHide"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoHideProperty =
            DependencyProperty.Register(nameof(AutoHide), typeof(bool), typeof(Compass), new PropertyMetadata(true, OnAutoHidePropertyChanged));

        private static void OnAutoHidePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = (Compass)d;
            compass.UpdateCompassRotation(false);
        }

        private void UpdateCompassRotation(bool useTransitions)
        {
            double heading = Heading;
            if (double.IsNaN(heading))
            {
                heading = 0;
            }

            var transform = GetTemplateChild("RotateTransform") as RotateTransform;
            if (transform != null)
            {
                transform.Angle = -heading;
            }

            bool autoHide = AutoHide && !DesignTime.IsDesignMode;
            if (Math.Round(heading % 360) == 0 && autoHide)
            {
                if (_isVisible)
                {
                    _isVisible = false;
                    VisualStateManager.GoToState(this, "HideCompass", useTransitions);
                }
            }
            else if (!_isVisible)
            {
                _isVisible = true;
                VisualStateManager.GoToState(this, "ShowCompass", useTransitions);
            }
        }
    }
}
#endif