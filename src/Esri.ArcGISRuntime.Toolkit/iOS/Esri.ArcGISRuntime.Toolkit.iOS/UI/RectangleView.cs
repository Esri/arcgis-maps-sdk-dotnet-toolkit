// /*******************************************************************************
//  * Copyright 2017 Esri
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

using CoreGraphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Draws a rectangle on the screen
    /// </summary>
    internal class RectangleView : UIView, INotifyPropertyChanged
    {
        public RectangleView() { }

        public RectangleView(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets or sets the rectangle's width
        /// </summary>
        public double Width
        {
            get => _size.Width;
            set
            {
                _size.Width = (nfloat)value;
                InvalidateIntrinsicContentSize();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the rectangle's height
        /// </summary>
        public double Height
        {
            get => _size.Height;
            set
            {
                _size.Height = (nfloat)value;
                InvalidateIntrinsicContentSize();
                OnPropertyChanged();
            }
        }

        private CGSize _size = CGSize.Empty;

        public override CGSize IntrinsicContentSize => _size;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
