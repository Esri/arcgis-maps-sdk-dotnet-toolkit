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

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal
{
    internal sealed class CalciteImageButton : ImageButton
    {
        private readonly string _glyph;
        public CalciteImageButton(string glyph)
        {
            _glyph = glyph;
            Source = new FontImageSource() { Glyph = _glyph, FontFamily = "calcite-ui-icons-24", Color = Color };
            this.SetAppThemeColor(ColorProperty, Colors.Black, Colors.White);
        }

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(CalciteImageButton), Colors.Black,
            propertyChanged: (s, oldValue, newValue) => ((CalciteImageButton)s).SetColor());

        private void SetColor()
        {
            ((FontImageSource)Source).Color = Color;
        }
    }
}
