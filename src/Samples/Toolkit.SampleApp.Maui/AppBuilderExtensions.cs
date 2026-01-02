// Parts of this file have been adapted from the .NET MAUI Project of the .NET Foundation (https://github.com/dotnet/maui).
//
// The.NET MAUI license is as follows:
//
// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Toolkit.SampleApp.Maui;

internal static class AppBuilderExtensions
{
    public static MauiAppBuilder UseWindowsAutomationTreeFix(this MauiAppBuilder builder)
    {
#if WINDOWS
        Microsoft.Maui.Handlers.ViewHandler.ViewMapper.AppendToMapping(nameof(IView.AutomationId), MapAutomationId);
#endif
        return builder;
    }


#if WINDOWS
    private static void MapAutomationId(IViewHandler handler, IView view)
    {
        // Workaround for https://github.com/dotnet/maui/issues/4715; Layouts, Pages and ContentViews are not exposed in AutomationTree
        if (string.IsNullOrEmpty(view.AutomationId) || view is Microsoft.Maui.ILayout or TemplatedView or Page or Border)
        {
            var platformView = (Microsoft.UI.Xaml.FrameworkElement?)handler.PlatformView;
            if (platformView is not null)
            {
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(platformView, view.AutomationId);
            }
        }
    }
#endif
}
