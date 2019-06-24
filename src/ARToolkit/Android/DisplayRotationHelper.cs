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

#if __ANDROID__
using System;
using Android.Content;
using Android.Hardware.Display;
using Android.Views;
using Java.Interop;

namespace Esri.ArcGISRuntime.ARToolkit
{
    internal class DisplayRotationHelper : Java.Lang.Object, DisplayManager.IDisplayListener
    {
        private bool _viewportChanged;
        private int _viewportWidth;
        private int _viewportHeight;
        private Context _context;
        private Display _display;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayRotationHelper"/> class, but does not register the listener yet.
        /// </summary>
        /// <param name="context">The Android Context</param>
        public DisplayRotationHelper(Context context)
        {
            _context = context;
            _display = context.GetSystemService(Java.Lang.Class.FromType(typeof(IWindowManager)))
                              .JavaCast<IWindowManager>().DefaultDisplay;
        }

        /// <summary>
        /// Registers the display listener. Should be called from {@link Activity#onResume()}.
        /// </summary>
        public void OnResume()
        {
            _context.GetSystemService(Java.Lang.Class.FromType(typeof(DisplayManager)))
                    .JavaCast<DisplayManager>().RegisterDisplayListener(this, null);
        }

        /// <summary>
        /// Unregisters the display listener. Should be called from {@link Activity#onPause()}.
        /// </summary>
        public void OnPause()
        {
            _context.GetSystemService(Java.Lang.Class.FromType(typeof(DisplayManager)))
                .JavaCast<DisplayManager>().UnregisterDisplayListener(this);
        }

        /// <summary>
        /// Records a change in surface dimensions. This will be later used by <see cref="UpdateSessionIfNeeded(Google.AR.Core.Session)" />.
        /// Should be called from <see cref="Android.Opengl.GLSurfaceView.IRenderer"/>
        /// </summary>
        /// <param name="width">The updated width of the surface.</param>
        /// <param name="height">The updated height of the surface.</param>
        public void OnSurfaceChanged(int width, int height)
        {
            _viewportWidth = width;
            _viewportHeight = height;
            _viewportChanged = true;
        }

        /// <summary>
        /// Updates the session display geometry if a change was posted either by
        /// <see cref="OnSurfaceChanged(int, int)"/> call or by <see cref="OnDisplayChanged(int)"/>
        /// system callback. This function should be called explicitly before each call to
        /// <see cref="Google.AR.Core.Session.Update"/> This function will also clear the 'pending update'
        /// (viewportChanged) flag.</summary>
        /// <param name="session">the Session object to update if display geometry changed.</param>
        public void UpdateSessionIfNeeded(Google.AR.Core.Session session)
        {
            if (_viewportChanged)
            {
                int displayRotation = (int)_display.Rotation;
                session.SetDisplayGeometry(displayRotation, _viewportWidth, _viewportHeight);
                _viewportChanged = false;
            }
        }

        /// <summary>
        /// Returns the current rotation state of android display.
        /// </summary>
        /// <returns>Display rotation</returns>
        public int Rotation() => (int)_display.Rotation;

        public void OnDisplayAdded(int displayId)
        {
        }

        public void OnDisplayRemoved(int displayId)
        {
        }

        public void OnDisplayChanged(int displayId)
        {
            _viewportChanged = true;
        }
    }
}
#endif