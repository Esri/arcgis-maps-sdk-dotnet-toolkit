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
using System;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Primitives;
using Xamarin.Forms;

namespace Toolkit.Samples.Forms.Samples.Helpers
{
    /// <summary>
    /// Layout used to acheive dynamic resize behavior.
    /// </summary>
    public class BottomSheetLayout : Layout<View>
    {
        private PanGestureRecognizer _sheetGestureRecognizer;
        private double _currentSwipeOffset;
        private double _minimumSheetHeight = 120;
        private double _availableSwipeDistance = 320;
        private double _lastTotalY;
        private bool _ignoreChurn = false;
        private double _fullHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="BottomSheetLayout"/> class.
        /// </summary>
        public BottomSheetLayout()
        {
            Padding = 16;
            _sheetGestureRecognizer = new PanGestureRecognizer() {  };
            _sheetGestureRecognizer.PanUpdated += SheetGestureRecognizer_PanUpdated;
        }

        private void SheetGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (_ignoreChurn)
            {
                return;
            }

            if (e.StatusType == GestureStatus.Running)
            {
                var diff = e.TotalY - _lastTotalY;
                _currentSwipeOffset = Math.Max(Math.Min(_currentSwipeOffset - diff, _availableSwipeDistance), 0);
                if (Device.RuntimePlatform != Device.Android || IsHorizontal)
                {
                    _lastTotalY = e.TotalY;
                }

                InvalidateLayout();
            }

            if (e.StatusType == GestureStatus.Completed)
            {
                _lastTotalY = 0;

                // Animate to snapped position
            }
        }

        /// <inheritdoc />
        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            var fullWidth = width + Padding.Left + Padding.Right;
            _fullHeight = height + Padding.Top + Padding.Bottom;
            _ignoreChurn = true;
            IsHorizontal = width > WidthBreakpoint;
            _availableSwipeDistance = _fullHeight - _minimumSheetHeight;
            double bottomSheetHeight = 0;
            double bottomSheetWidth = 0;
            MapView savedMapView = null;
            foreach (var child in Children)
            {
                if (child is MapView mapview)
                {
                    LayoutChildIntoBoundingRegion(mapview, new Rectangle(0, 0, fullWidth, _fullHeight));
                    savedMapView = mapview;
                }
                else if ((bool)child.GetValue(IsBottomSheetProperty))
                {
                    if (IsHorizontal)
                    {
                        bottomSheetWidth = Math.Min(320, height);
                        LayoutChildIntoBoundingRegion(child, new Rectangle(0, 0, bottomSheetWidth, Math.Max(_fullHeight - _currentSwipeOffset, _minimumSheetHeight)));
                    }
                    else
                    {
                        bottomSheetHeight = Math.Min(Math.Max(_minimumSheetHeight + _currentSwipeOffset, _minimumSheetHeight), height);
                        LayoutChildIntoBoundingRegion(child, new Rectangle(0, height - bottomSheetHeight + Padding.Bottom + Padding.Top, width + Padding.Left + Padding.Right, bottomSheetHeight));
                    }

                    if (!child.GestureRecognizers.Contains(_sheetGestureRecognizer))
                    {
                        child.GestureRecognizers.Add(_sheetGestureRecognizer);
                    }
                }
            }

            // Apply insets
            if (savedMapView != null && !IsHorizontal)
            {
                savedMapView.ViewInsets = new Thickness(0, 0, 0, bottomSheetHeight);
            }
            else if (IsHorizontal && savedMapView != null)
            {
                savedMapView.ViewInsets = new Thickness(bottomSheetWidth, 0, 0, 0);
            }

            _ignoreChurn = false;
        }

        /// <inheritdoc />
        protected override void OnChildRemoved(Element child, int oldLogicalIndex)
        {
            base.OnChildRemoved(child, oldLogicalIndex);
            if (child is View viewChild && viewChild.GestureRecognizers.Contains(_sheetGestureRecognizer))
            {
                viewChild.GestureRecognizers.Remove(_sheetGestureRecognizer);
            }
        }

        /// <summary>
        /// Gets or sets the breakpoint that determines whether the view is horizontal or vertical.
        /// </summary>
        public double WidthBreakpoint
        {
            get => (double)GetValue(WidthBreakpointProperty);
            set => SetValue(WidthBreakpointProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="WidthBreakpoint"/> bindable property.
        /// </summary>
        public static readonly BindableProperty WidthBreakpointProperty =
            BindableProperty.Create("WidthBreakpoint", typeof(double), typeof(BottomSheetLayout), 549.0, propertyChanged: (bindable, oldvalue, newvalue) =>
            {
                ((BottomSheetLayout)bindable).InvalidateLayout();
            });

        /// <summary>
        /// Identifies the 'IsBottomSheet' attached property.
        /// </summary>
        public static readonly BindableProperty IsBottomSheetProperty =
            BindableProperty.CreateAttached("IsBottomSheet", typeof(bool), typeof(BottomSheetLayout), false);

        /// <summary>
        /// Gets the attached property.
        /// </summary>
        public static bool GetIsBottomSheet(BindableObject target) => (bool)target.GetValue(IsBottomSheetProperty);

        /// <summary>
        /// Gets the attached property.
        /// </summary>
        public static void SetIsBottomSheet(BindableObject target, bool value) => target.SetValue(IsBottomSheetProperty, value);

        internal static readonly BindablePropertyKey IsHorizontalPropertyKey =
            BindableProperty.CreateReadOnly("IsHorizontal", typeof(bool), typeof(BottomSheetLayout), false);

        /// <summary>
        /// Identifies the <see cref="IsHorizontal"/> read-only bindable property.
        /// </summary>
        public static readonly BindableProperty IsHorizontalProperty = IsHorizontalPropertyKey.BindableProperty;

        /// <summary>
        /// Gets a value indicating whether the layout is in a horizontal orientation.
        /// </summary>
        public bool IsHorizontal
        {
            get => (bool)GetValue(IsHorizontalProperty);
            private set => SetValue(IsHorizontalPropertyKey, value);
        }

        /// <summary>
        /// Gets or sets the requested size/pose for the bottom sheet.
        /// </summary>
        public ElementLayoutSizePreference RequestedSheetSize
        {
            get => (ElementLayoutSizePreference)GetValue(RequestedSheetSizeProperty);
            set => SetValue(RequestedSheetSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RequestedSheetSize"/> bindable property.
        /// </summary>
        public static readonly BindableProperty RequestedSheetSizeProperty =
            BindableProperty.Create(nameof(RequestedSheetSize), typeof(ElementLayoutSizePreference), typeof(BottomSheetLayout), ElementLayoutSizePreference.NotSet, propertyChanged: HandleRequestedSizeChanged);

        private static void HandleRequestedSizeChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is BottomSheetLayout bottomSheetParent && newValue is ElementLayoutSizePreference preference)
            {
                switch (preference)
                {
                    case ElementLayoutSizePreference.Collapsed:
                        bottomSheetParent._currentSwipeOffset = bottomSheetParent.IsHorizontal ? bottomSheetParent._availableSwipeDistance : 0;
                        break;
                    case ElementLayoutSizePreference.Half:
                        bottomSheetParent._currentSwipeOffset = bottomSheetParent.IsHorizontal ? 0 : bottomSheetParent._availableSwipeDistance / 2;
                        break;
                    case ElementLayoutSizePreference.Maximized:
                        bottomSheetParent._currentSwipeOffset = bottomSheetParent.IsHorizontal ? 0 : bottomSheetParent._availableSwipeDistance;
                        break;
                }

                bottomSheetParent.InvalidateLayout();
            }
        }
    }
}
