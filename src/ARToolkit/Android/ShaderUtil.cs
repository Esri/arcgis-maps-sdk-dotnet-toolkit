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
using System.IO;
using Android.Content;
using Android.Opengl;
using Android.Util;

namespace Esri.ArcGISRuntime.ARToolkit
{
    internal static class ShaderUtil
    {
        public static int LoadGLShader(Context context, int type, int resId)
        {
            var code = ReadRawTextFile(context, resId);
            return LoadGLShader(type, code);
        }

        public static int LoadGLShader(int type, string code)
        {
            var shader = GLES20.GlCreateShader(type);

            GLES20.GlShaderSource(shader, code);
            GLES20.GlCompileShader(shader);

            var compileStatus = new int[1];
            GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, compileStatus, 0);

            if (compileStatus[0] == 0)
            {
                GLES20.GlDeleteShader(shader);
                shader = 0;
            }

            if (shader == 0)
            {
                throw new Exception("Error creating shader");
            }

            return shader;
        }

        public static void CheckGLError(string tag, string label)
        {
            int error;
            while ((error = GLES20.GlGetError()) != GLES20.GlNoError)
            {
                Log.Error(tag, label + ": glError " + error);
                throw new Exception(label + ": glError " + error);
            }
        }

        private static string ReadRawTextFile(Context context, int resId)
        {
            string result = null;

            using (var rs = context.Resources.OpenRawResource(resId))
            using (var sr = new StreamReader(rs))
            {
                result = sr.ReadToEnd();
            }

            return result;
        }
    }
}
#endif