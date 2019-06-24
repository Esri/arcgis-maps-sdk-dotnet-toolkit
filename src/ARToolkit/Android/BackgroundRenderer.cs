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
using Android.Opengl;
using Google.AR.Core;
using Java.Nio;

namespace Esri.ArcGISRuntime.ARToolkit
{
    /// <summary>
    /// Renders the camera view
    /// </summary>
    internal class BackgroundRenderer
    {
        private const string TAG = "BACKGROUNDRENDERER";

        private const int CoordPerVertex = 2;
        private const int TexCoordsPerVertex = 2;
        private const int FloatSize = 4;

        private FloatBuffer _quadCoords;
        private FloatBuffer _quadTexCoords;
        private int _quadProgram;
        private int _quadPositionParam;
        private int _quadTexCoordParam;
        private int _textureTarget = GLES11Ext.GlTextureExternalOes;
        private static readonly float[] QuadCoords = new float[] { -1.0f, -1.0f, -1.0f, +1.0f, +1.0f, -1.0f, +1.0f, +1.0f, };

        public BackgroundRenderer()
        {
        }

        public int TextureId { get; private set; } = -1;

        /// <summary>
        /// Allocates and initializes OpenGL resources needed by the background renderer.
        /// Must be called on the OpenGL thread, typically in <see cref="GLSurfaceView.IRenderer.OnSurfaceChanged(Javax.Microedition.Khronos.Opengles.IGL10, int, int)" />
        /// </summary>
        /// <param name="context">Needed to access shader source.</param>
        public void CreateOnGlThread(Context context)
        {
            // Generate the background texture.
            var textures = new int[1];
            GLES20.GlGenTextures(1, textures, 0);
            TextureId = textures[0];
            GLES20.GlBindTexture(_textureTarget, TextureId);
            GLES20.GlTexParameteri(_textureTarget, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(_textureTarget, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(_textureTarget, GLES20.GlTextureMinFilter, GLES20.GlLinear);
            GLES20.GlTexParameteri(_textureTarget, GLES20.GlTextureMagFilter, GLES20.GlLinear);

            int numVertices = 4;
            if (numVertices != QuadCoords.Length / CoordPerVertex)
            {
                throw new Exception("Unexpected number of vertices in BackgroundRenderer.");
            }

            var bbCoords = ByteBuffer.AllocateDirect(QuadCoords.Length * FloatSize);
            bbCoords.Order(ByteOrder.NativeOrder());
            _quadCoords = bbCoords.AsFloatBuffer();
            _quadCoords.Put(QuadCoords);
            _quadCoords.Position(0);

            var bbTexCoords = ByteBuffer.AllocateDirect(numVertices * TexCoordsPerVertex * FloatSize);
            bbTexCoords.Order(ByteOrder.NativeOrder());
            _quadCoords = bbTexCoords.AsFloatBuffer();
            _quadCoords.Put(QuadCoords);
            _quadCoords.Position(0);

            var bbTexCoordsTransformed = ByteBuffer.AllocateDirect(numVertices * TexCoordsPerVertex * FloatSize);
            bbTexCoordsTransformed.Order(ByteOrder.NativeOrder());
            _quadTexCoords = bbTexCoordsTransformed.AsFloatBuffer();

            int vertexShader = ShaderUtil.LoadGLShader(TAG, context, GLES20.GlVertexShader, Resource.Raw.screenquad_vertex);
            int fragmentShader = ShaderUtil.LoadGLShader(TAG, context, GLES20.GlFragmentShader, Resource.Raw.screenquad_fragment_oes);

            _quadProgram = GLES20.GlCreateProgram();
            GLES20.GlAttachShader(_quadProgram, vertexShader);
            GLES20.GlAttachShader(_quadProgram, fragmentShader);
            GLES20.GlLinkProgram(_quadProgram);
            GLES20.GlUseProgram(_quadProgram);

            ShaderUtil.CheckGLError(TAG, "Program creation");

            _quadPositionParam = GLES20.GlGetAttribLocation(_quadProgram, "a_Position");
            _quadTexCoordParam = GLES20.GlGetAttribLocation(_quadProgram, "a_TexCoord");

            ShaderUtil.CheckGLError(TAG, "Program parameters");
        }

        /// <summary>
        ///  Draws the AR background image.  The image will be drawn such that virtual content rendered
        ///  with the matrices provided by Frame getViewMatrix(float[], int) and  Session getProjectionMatrix(float[], int, float, float)}
        ///  will accurately follow static physical objects. This must be called <b>before</b> drawing virtual content.
        /// </summary>
        /// <param name="frame">The last frame returned by <see cref="Session.Update"/></param>
        public void Draw(Frame frame)
        {
            // If display rotation changed (also includes view size change), we need to re-query the uv
            // coordinates for the screen rect, as they may have changed as well.
            if (frame.HasDisplayGeometryChanged)
            {
                frame.TransformCoordinates2d(Coordinates2d.OpenglNormalizedDeviceCoordinates, _quadCoords, Coordinates2d.TextureNormalized, _quadTexCoords); // mQuadTexCoordTransformed
                // frame.TransformCoordinates2d()
            }

            if (frame.Timestamp == 0)
            {
                // Suppress rendering if the camera did not produce the first frame yet. This is to avoid
                // drawing possible leftover data from previous sessions if the texture is reused.
                return;
            }

            Draw();
        }

        private void Draw()
        {
            // Ensure position is rewound before use.
            _quadCoords.Position(0);

            // No need to test or write depth, the screen quad has arbitrary depth, and is expected
            // to be drawn first.
            GLES20.GlDisable(GLES20.GlDepthTest);
            GLES20.GlDepthMask(false);

            GLES20.GlBindTexture(GLES11Ext.GlTextureExternalOes, TextureId);

            GLES20.GlUseProgram(_quadProgram);

            // Set the vertex positions.
            GLES20.GlVertexAttribPointer(_quadPositionParam, CoordPerVertex, GLES20.GlFloat, false, 0, _quadCoords);

            // Set the texture coordinates.
            GLES20.GlVertexAttribPointer(_quadTexCoordParam, TexCoordsPerVertex, GLES20.GlFloat, false, 0, _quadTexCoords);

            // Enable vertex arrays
            GLES20.GlEnableVertexAttribArray(_quadPositionParam);
            GLES20.GlEnableVertexAttribArray(_quadTexCoordParam);

            GLES20.GlDrawArrays(GLES20.GlTriangleStrip, 0, 4);

            // Disable vertex arrays
            GLES20.GlDisableVertexAttribArray(_quadPositionParam);
            GLES20.GlDisableVertexAttribArray(_quadTexCoordParam);

            // Restore the depth state for further drawing.
            GLES20.GlDepthMask(true);
            GLES20.GlEnable(GLES20.GlDepthTest);

            ShaderUtil.CheckGLError(TAG, "Draw");
        }
    }
}
#endif