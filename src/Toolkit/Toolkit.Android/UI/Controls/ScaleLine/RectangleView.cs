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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Android.Content;
using Android.Graphics;
using Android.Views;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Draws a rectangle on the screen.
    /// </summary>
    /// <remarks>Provides a convenient mechanism for rendering rectangle elements of a certain size. The
    /// specified width and height will be applied to the width and height of the view's layout parameters.</remarks>
    internal class RectangleView : View, INotifyPropertyChanged
    {
        public RectangleView(Context context)
            : base(context)
        {
        }

        public RectangleView(Context context, double width, double height)
            : this(context)
        {
            Width = width;
            Height = height;
        }

        private double _width;

        /// <summary>
        /// Gets or sets the rectangle's width.
        /// </summary>
        public new double Width
        {
            get => _width;
            set
            {
                _width = value;
                var layoutParams = GetLayoutParams();
                layoutParams.Width = (int)Math.Round(value);
                LayoutParameters = layoutParams;
                OnPropertyChanged();
            }
        }

        private ViewGroup.LayoutParams GetLayoutParams()
        {
            if (LayoutParameters == null)
            {
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent);
            }

            return LayoutParameters;
        }

        private double _height;

        /// <summary>
        /// Gets or sets the rectangle's height.
        /// </summary>
        public new double Height
        {
            get => _height;
            set
            {
                _height = value;
                var layoutParams = GetLayoutParams();
                layoutParams.Height = (int)Math.Round(value);
                LayoutParameters = layoutParams;
                OnPropertyChanged();
            }
        }

        private Color _backgroundColor;

        /// <summary>
        /// Gets or sets the rectangle's background color.
        /// </summary>
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                SetBackgroundColor(value);
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
