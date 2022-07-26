using System;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Primitives
{
    public class BottomSheetLayout : Layout<View>
    {
        private PanGestureRecognizer _sheetGestureRecognizer;
        private double _currentSwipeOffset;
        private double _minimumSheetHeight = 120;
        private double _availableSwipeDistance = 320;
        private double _lastTotalY;
        private bool _ignoreChurn = false;
        private double _fullHeight;


        public BottomSheetLayout()
        {
            Padding = 16;
            _sheetGestureRecognizer = new PanGestureRecognizer() {  };
            _sheetGestureRecognizer.PanUpdated += _sheetGestureRecognizer_PanUpdated;
        }

        private void _sheetGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
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

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            var fullWidth = width + Padding.Left + Padding.Right;
            _fullHeight = height + Padding.Top + Padding.Bottom;
            _ignoreChurn = true;
            IsHorizontal = width > WidthBreakpoint;
            _availableSwipeDistance = _fullHeight - _minimumSheetHeight;
            double _bottomSheetHeight = 0;
            double _bottomSheetWidth = 0;
            MapView? savedMapView = null;
            foreach (var child in Children)
            {
                if (child is MapView mapview)
                {
                    LayoutChildIntoBoundingRegion(mapview, new Rectangle(0, 0, fullWidth, _fullHeight));
                    savedMapView = mapview;
                }
                // Accessories laid out within space above or to the right of the floating panel according to horizontal and vertical layout options
                // Accessories hidden when bottom sheet is expanded
                else if ((bool)child.GetValue(IsAccessoryProperty) == true)
                {
                }
                // Bottom sheet
                else if ((bool)child.GetValue(IsBottomSheetProperty))
                {
                    if (IsHorizontal)
                    {
                        _bottomSheetWidth = Math.Min(320, height);
                        LayoutChildIntoBoundingRegion(child, new Rectangle(0, 0, _bottomSheetWidth, Math.Max(_fullHeight - _currentSwipeOffset, _minimumSheetHeight)));
                    }
                    else
                    {
                        _bottomSheetHeight = Math.Min(Math.Max(_minimumSheetHeight + _currentSwipeOffset, _minimumSheetHeight), height);
                        LayoutChildIntoBoundingRegion(child, new Rectangle(0, height - _bottomSheetHeight + Padding.Bottom + Padding.Top, width + Padding.Left + Padding.Right, _bottomSheetHeight));
                        //child.LayoutTo(new Rectangle(0, height - newHeight + Padding.Bottom, width + Padding.Left + Padding.Right, newHeight), 0, null);
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
                savedMapView.ViewInsets = new Thickness(0, 0, 0, _bottomSheetHeight);
            }
            else if (IsHorizontal)
            {
                savedMapView.ViewInsets = new Thickness(_bottomSheetWidth, 0,0,0);
            }
            _ignoreChurn = false;
        }

        protected override void OnChildRemoved(Element child, int oldLogicalIndex)
        {
            base.OnChildRemoved(child, oldLogicalIndex);
            if (child is View viewChild && viewChild.GestureRecognizers.Contains(_sheetGestureRecognizer))
            {
                viewChild.GestureRecognizers.Remove(_sheetGestureRecognizer);
            }
        }

        public double WidthBreakpoint
        {
            get => (double)GetValue(WidthBreakpointProperty);
            set => SetValue(WidthBreakpointProperty, value);
        }
        public double CollapsedHeight
        {
            get => (double)GetValue(CollapsedHeightProperty);
            set => SetValue(CollapsedHeightProperty, value);
        }

        public static readonly BindableProperty WidthBreakpointProperty =
               BindableProperty.Create("WidthBreakpoint", typeof(double), typeof(BottomSheetLayout), 549.0, propertyChanged: (bindable, oldvalue, newvalue) =>
               {
                   ((BottomSheetLayout)bindable).InvalidateLayout();
               });

        public static readonly BindableProperty CollapsedHeightProperty =
            BindableProperty.Create("CollapsedHeight", typeof(double), typeof(BottomSheetLayout), 12.0);

        public static readonly BindableProperty LayoutPreferenceProperty =
            BindableProperty.CreateAttached("LayoutPreference", typeof(string), typeof(BottomSheetLayout), "notset", BindingMode.Default, null, HandleDeferenceChange, null, null, null);

        public static readonly BindableProperty IsBottomSheetProperty =
            BindableProperty.CreateAttached("IsBottomSheet", typeof(bool), typeof(BottomSheetLayout), false);

        public static bool GetIsBottomSheet(BindableObject target) => (bool)target.GetValue(IsBottomSheetProperty);
        public static void SetIsBottomSheet(BindableObject target, bool value) => target.SetValue(IsBottomSheetProperty, value);

        public static readonly BindableProperty IsAccessoryProperty =
            BindableProperty.CreateAttached("IsAccessory", typeof(bool), typeof(BottomSheetLayout), false, BindingMode.Default);

        internal static readonly BindablePropertyKey IsHorizontalPropertyKey =
            BindableProperty.CreateReadOnly("IsHorizontal", typeof(bool), typeof(BottomSheetLayout), false);

        public static readonly BindableProperty IsHorizontalProperty = IsHorizontalPropertyKey.BindableProperty;

        public bool IsHorizontal
        {
            get => (bool)GetValue(IsHorizontalProperty);
            private set => SetValue(IsHorizontalPropertyKey, value);
        }


        private static void HandleDeferenceChange(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is Element sendingView)
            {
                while (sendingView.Parent != null)
                {
                    if (sendingView.Parent is BottomSheetLayout bottomSheetParent)
                    {
                        var prefString = newValue?.ToString();
                        if (prefString == "collapsed")
                        {
                            bottomSheetParent._currentSwipeOffset = bottomSheetParent.IsHorizontal ? bottomSheetParent._availableSwipeDistance : 0;
                        }
                        else if (prefString == "5050")
                        {
                            bottomSheetParent._currentSwipeOffset = bottomSheetParent.IsHorizontal ? 0 : bottomSheetParent._availableSwipeDistance / 2;
                        }
                        else if (prefString == "maximized")
                        {
                            bottomSheetParent._currentSwipeOffset = bottomSheetParent.IsHorizontal ? 0 : bottomSheetParent._availableSwipeDistance;
                        }
                        
                        bottomSheetParent.InvalidateLayout();
                    }

                    sendingView = sendingView.Parent;
                }
            }
        }
    }
}
