#if __ANDROID__
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.Views;
using Java.Nio;
using Javax.Microedition.Khronos.Opengles;
using Matrix = Android.Opengl.Matrix;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

internal class Image360Renderer :  Java.Lang.Object, GLSurfaceView.IRenderer
{
    readonly Context _context;

    int _program;
    int _textureId;

    int _aPosition;
    int _aTexCoord;
    int _uMvp;
    int _uTexture;

    FloatBuffer? _vertexBuffer;
    FloatBuffer? _texCoordBuffer;
    ShortBuffer? _indexBuffer;
    int _indexCount;

    readonly float[] _projection = new float[16];
    readonly float[] _view = new float[16];
    readonly float[] _model = new float[16];
    readonly float[] _mvp = new float[16];
    readonly float[] _temp = new float[16];

    float _yaw;
    float _pitch;
    byte[]? _pendingAsset;

    public Image360Renderer(Context context)
    {
        _context = context;
    }

    public void SetImage(Stream stream)
    {
        if (stream is null)
            _pendingAsset = null;
        else
        {
            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);

            _pendingAsset = ms.ToArray();

            if (_program != 0)
                LoadTextureFromImageData(_pendingAsset);
        }
    }

    public void AddRotation(float dx, float dy)
    {
        _yaw += dx * 0.15f;
        _pitch += dy * 0.15f;
        _pitch = Math.Clamp(_pitch, -89f, 89f);
    }

    public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
    {
        GLES20.GlClearColor(0, 0, 0, 1);
        GLES20.GlDisable(GLES20.GlDepthTest);
        GLES20.GlDisable(GLES20.GlCullFaceMode);

        _program = CreateProgram(VertexShader, FragmentShader);

        _aPosition = GLES20.GlGetAttribLocation(_program, "a_Position");
        _aTexCoord = GLES20.GlGetAttribLocation(_program, "a_TexCoord");
        _uMvp = GLES20.GlGetUniformLocation(_program, "u_MVP");
        _uTexture = GLES20.GlGetUniformLocation(_program, "u_Texture");

        CreateSphere(64, 32);

        Matrix.SetIdentityM(_model, 0);

        if (_pendingAsset != null)
            LoadTextureFromImageData(_pendingAsset!);
    }

    public void OnSurfaceChanged(IGL10? gl, int width, int height)
    {
        GLES20.GlViewport(0, 0, width, height);

        var aspect = width / (float)Math.Max(height, 1);
        Matrix.PerspectiveM(_projection, 0, 75f, aspect, 0.1f, 100f);
    }

    public void OnDrawFrame(IGL10? gl)
    {
        GLES20.GlClear(GLES20.GlColorBufferBit);

        Matrix.SetIdentityM(_view, 0);
        Matrix.RotateM(_view, 0, -_pitch, 1, 0, 0);
        Matrix.RotateM(_view, 0, -_yaw, 0, 1, 0);

        Matrix.MultiplyMM(_temp, 0, _view, 0, _model, 0);
        Matrix.MultiplyMM(_mvp, 0, _projection, 0, _temp, 0);

        GLES20.GlUseProgram(_program);

        GLES20.GlUniformMatrix4fv(_uMvp, 1, false, _mvp, 0);

        GLES20.GlActiveTexture(GLES20.GlTexture0);
        GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
        GLES20.GlUniform1i(_uTexture, 0);

        _vertexBuffer!.Position(0);
        GLES20.GlEnableVertexAttribArray(_aPosition);
        GLES20.GlVertexAttribPointer(_aPosition, 3, GLES20.GlFloat, false, 0, _vertexBuffer);

        _texCoordBuffer!.Position(0);
        GLES20.GlEnableVertexAttribArray(_aTexCoord);
        GLES20.GlVertexAttribPointer(_aTexCoord, 2, GLES20.GlFloat, false, 0, _texCoordBuffer);

        _indexBuffer!.Position(0);
        GLES20.GlDrawElements(GLES20.GlTriangles, _indexCount, GLES20.GlUnsignedShort, _indexBuffer);

        GLES20.GlDisableVertexAttribArray(_aPosition);
        GLES20.GlDisableVertexAttribArray(_aTexCoord);
    }

    void CreateSphere(int longitudeSegments, int latitudeSegments)
    {
        var vertices = new List<float>();
        var texCoords = new List<float>();
        var indices = new List<short>();

        const float radius = 10f;

        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            var v = lat / (float)latitudeSegments;
            var theta = v * MathF.PI;

            var sinTheta = MathF.Sin(theta);
            var cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                var u = lon / (float)longitudeSegments;
                var phi = u * MathF.PI * 2f;

                var sinPhi = MathF.Sin(phi);
                var cosPhi = MathF.Cos(phi);

                var x = radius * sinTheta * sinPhi;
                var y = radius * cosTheta;
                var z = radius * sinTheta * cosPhi;

                vertices.Add(x);
                vertices.Add(y);
                vertices.Add(z);

                // Flip U because we are viewing from inside the sphere.
                texCoords.Add(1f - u);
                texCoords.Add(v);
            }
        }

        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                var first = (short)(lat * (longitudeSegments + 1) + lon);
                var second = (short)(first + longitudeSegments + 1);

                // Reversed winding for inside-facing sphere.
                indices.Add(first);
                indices.Add((short)(first + 1));
                indices.Add(second);

                indices.Add(second);
                indices.Add((short)(first + 1));
                indices.Add((short)(second + 1));
            }
        }

        _vertexBuffer = ToFloatBuffer(vertices.ToArray());
        _texCoordBuffer = ToFloatBuffer(texCoords.ToArray());
        _indexBuffer = ToShortBuffer(indices.ToArray());
        _indexCount = indices.Count;
    }

    void LoadTextureFromImageData(byte[] imagedata)
    {
        if (_textureId != 0)
            GLES20.GlDeleteTextures(1, new[] { _textureId }, 0);

        var textureIds = new int[1];
        GLES20.GlGenTextures(1, textureIds, 0);
        _textureId = textureIds[0];

        GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);

        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, GLES20.GlLinear);
        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, GLES20.GlLinear);
        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
        GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);

        using var stream = new MemoryStream(imagedata);
        using var bitmap = BitmapFactory.DecodeStream(stream);

        GLUtils.TexImage2D(GLES20.GlTexture2d, 0, bitmap, 0);

        GLES20.GlBindTexture(GLES20.GlTexture2d, 0);
    }

    static int CreateProgram(string vertexShaderSource, string fragmentShaderSource)
    {
        var vertexShader = CompileShader(GLES20.GlVertexShader, vertexShaderSource);
        var fragmentShader = CompileShader(GLES20.GlFragmentShader, fragmentShaderSource);

        var program = GLES20.GlCreateProgram();
        GLES20.GlAttachShader(program, vertexShader);
        GLES20.GlAttachShader(program, fragmentShader);
        GLES20.GlLinkProgram(program);

        var linkStatus = new int[1];
        GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, linkStatus, 0);

        if (linkStatus[0] == 0)
        {
            var log = GLES20.GlGetProgramInfoLog(program);
            GLES20.GlDeleteProgram(program);
            throw new InvalidOperationException($"OpenGL program link failed: {log}");
        }

        GLES20.GlDeleteShader(vertexShader);
        GLES20.GlDeleteShader(fragmentShader);

        return program;
    }

    static int CompileShader(int type, string source)
    {
        var shader = GLES20.GlCreateShader(type);
        GLES20.GlShaderSource(shader, source);
        GLES20.GlCompileShader(shader);

        var compileStatus = new int[1];
        GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, compileStatus, 0);

        if (compileStatus[0] == 0)
        {
            var log = GLES20.GlGetShaderInfoLog(shader);
            GLES20.GlDeleteShader(shader);
            throw new InvalidOperationException($"OpenGL shader compile failed: {log}");
        }

        return shader;
    }

    static FloatBuffer ToFloatBuffer(float[] data)
    {
        var buffer = ByteBuffer
            .AllocateDirect(data.Length * 4)
            .Order(ByteOrder.NativeOrder())
            .AsFloatBuffer();

        buffer.Put(data);
        buffer.Position(0);
        return buffer;
    }

    static ShortBuffer ToShortBuffer(short[] data)
    {
        var buffer = ByteBuffer
            .AllocateDirect(data.Length * 2)
            .Order(ByteOrder.NativeOrder())
            .AsShortBuffer();

        buffer.Put(data);
        buffer.Position(0);
        return buffer;
    }

    public void OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
    {
        GLES20.GlClearColor(0, 0, 0, 1);

        // We are rendering from inside the sphere, so culling can get confusing.
        // Easiest: disable culling.
        GLES20.GlDisable(GLES20.GlCullFaceMode);

        // Depth is not very useful here because we only draw one surrounding sphere.
        GLES20.GlDisable(GLES20.GlDepthTest);

        _program = CreateProgram(VertexShader, FragmentShader);

        _aPosition = GLES20.GlGetAttribLocation(_program, "a_Position");
        _aTexCoord = GLES20.GlGetAttribLocation(_program, "a_TexCoord");
        _uMvp = GLES20.GlGetUniformLocation(_program, "u_MVP");
        _uTexture = GLES20.GlGetUniformLocation(_program, "u_Texture");

        CreateSphere(64, 32);

        Matrix.SetIdentityM(_model, 0);

        if (_pendingAsset is not null)
            LoadTextureFromImageData(_pendingAsset);
        //_pendingAsset = null;
    }

    const string VertexShader = """
        uniform mat4 u_MVP;

        attribute vec3 a_Position;
        attribute vec2 a_TexCoord;

        varying vec2 v_TexCoord;

        void main()
        {
            v_TexCoord = a_TexCoord;
            gl_Position = u_MVP * vec4(a_Position, 1.0);
        }
        """;

    const string FragmentShader = """
        precision mediump float;

        uniform sampler2D u_Texture;

        varying vec2 v_TexCoord;

        void main()
        {
            gl_FragColor = texture2D(u_Texture, v_TexCoord);
        }
        """;
}
#endif