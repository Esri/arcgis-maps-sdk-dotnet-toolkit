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
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.RealTime;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics;
#if WPF
using System.Windows.Markup;
#elif WINUI
using Microsoft.UI.Xaml.Markup;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

#if WPF
[ContentProperty(nameof(Markers))]
#elif WINUI
[ContentProperty(Name = nameof(Markers))]
#endif
[TemplatePart(Name = ImageViewName, Type = typeof(Image))]
public partial class OrientedImageView : Control
{
    private const string ImageViewName = "Image";
}
#endif