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

#if MAUI
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageView : TemplatedView
{
    /// <summary>
    /// Template name of the <see cref="Image"/> view.
    /// </summary>
    public const string ImageViewName = "Image";

    private static readonly ControlTemplate DefaultControlTemplate;

    static OrientedImageView()
    {
        DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
    }

    private static object BuildDefaultTemplate()
    {
        Grid root = new Grid();
        Image image = new Image { Aspect = Aspect.AspectFit };
        root.Children.Add(image);
        INameScope nameScope = new NameScope();
        NameScope.SetNameScope(root, nameScope);
        nameScope.RegisterName(ImageViewName, image);

        return root;
    }
}

#endif