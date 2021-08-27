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
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Internal;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    internal static class UIViewControllerHelper
    {
        // Returns the top-most view controller in the key window.
        // We use this to ensure the alert appears above all other views.
        public static UIViewController GetTopViewController()
        {
            var keyWindow = UIApplication.SharedApplication.Windows.FirstOrDefault(window => window.IsKeyWindow);
            var rootViewController = keyWindow?.RootViewController;
            if (rootViewController is null)
            {
                // should not happen -- there's always a key window
                throw new Exception("Failed to get key window.");
            }

            if (rootViewController is UITabBarController tabBarController)
            {
                return tabBarController.SelectedViewController!;
            }
            else if (rootViewController is UINavigationController navigationController)
            {
                return navigationController.IsBeingDismissed
                    ? navigationController.VisibleViewController.PresentingViewController
                    : navigationController.VisibleViewController;
            }
            else if (rootViewController.PresentedViewController != null)
            {
                var presentedViewController = rootViewController.PresentedViewController;
                return presentedViewController.IsBeingDismissed
                    ? presentedViewController.PresentingViewController
                    : rootViewController;
            }
            else
            {
                return rootViewController;
            }
        }

        // MOdally presents the view controller on the top-most view.
        public static void PresentViewControllerOnTop(UIViewController viewController)
        {
            Dispatcher.RunAsyncAction(() =>
            {
                var topViewController = GetTopViewController();

                if (viewController.ModalPresentationStyle == UIModalPresentationStyle.Popover)
                {
                    viewController.PopoverPresentationController.SourceView = topViewController.View!;
                    viewController.PopoverPresentationController.SourceRect = topViewController.View!.Bounds;
                    viewController.PopoverPresentationController.PermittedArrowDirections = 0;
                    viewController.PopoverPresentationController.PassthroughViews = new[] { topViewController.View };
                }
                else if (viewController.ModalPresentationStyle == UIModalPresentationStyle.FormSheet)
                {
                    viewController.ModalInPresentation = true;
                }
                else if (viewController.ModalPresentationStyle != UIModalPresentationStyle.FullScreen)
                {
                    viewController.ModalInPresentation = true;
                    viewController.ModalPresentationStyle = UIModalPresentationStyle.Automatic;
                }

                topViewController.PresentViewController(viewController, true, null);
            });
        }
    }
}
