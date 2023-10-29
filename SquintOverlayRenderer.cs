namespace ZoomButton
{
    using Vintagestory.API.Client;

    /*
    REFERENCE:
    https://github.com/anegostudios/vsmodexamples/blob/master/Mods/ScreenOverlaySquintShader/src/ScreenOverlaySquintShader.cs
    https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83
    http://glslsandbox.com/e#71442.0
    https://www.geeks3d.com/20091020/shader-library-lens-circle-post-processing-effect-glsl/
    */

    public class SquintOverlayRenderer : IRenderer
    {
        readonly MeshRef quadRef;
        readonly ICoreClientAPI capi;
        public IShaderProgram overlayShaderProg;

        public float PercentZoomed = 0;

        public SquintOverlayRenderer(ICoreClientAPI capi)
        {
            this.capi = capi;
            var quadMesh = QuadMeshUtil.GetCustomQuadModelData(-1, -1, 0, 2, 2);
            quadMesh.Rgba = null;
            this.quadRef = capi.Render.UploadMesh(quadMesh);

            this.LoadShader();
            capi.Event.ReloadShader += this.LoadShader;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Ortho);
        }

        public double RenderOrder => 1.1;

        public int RenderRange => 1;

        public void Dispose()
        {
            this.capi.Render.DeleteMesh(this.quadRef);
            //1.18 why this crash
            //this.overlayShaderProg.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (this.PercentZoomed == 0)
            { return; }

            var curShader = this.capi.Render.CurrentActiveShader;
            curShader.Stop();
            this.overlayShaderProg.Use();
            this.capi.Render.GlToggleBlend(true);
            this.overlayShaderProg.Uniform("percentZoomed", this.PercentZoomed);
            this.capi.Render.RenderMesh(this.quadRef);
            this.overlayShaderProg.Stop();
            curShader.Use();
        }

        public bool LoadShader()
        {
            this.overlayShaderProg = this.capi.Shader.NewShaderProgram();
            this.overlayShaderProg.VertexShader = this.capi.Shader.NewShader(EnumShaderType.VertexShader);
            this.overlayShaderProg.FragmentShader = this.capi.Shader.NewShader(EnumShaderType.FragmentShader);

            this.overlayShaderProg.VertexShader.Code = this.GetVertexShaderCode();
            this.overlayShaderProg.FragmentShader.Code = this.GetFragmentShaderCode();

            this.capi.Shader.RegisterMemoryShaderProgram("squintoverlay", this.overlayShaderProg);
            this.overlayShaderProg.Compile();

            return true;
        }

        private string GetVertexShaderCode()
        {
            return @"
        #version 330 core
        #extension GL_ARB_explicit_attrib_location: enable

        #ifdef GL_ES
        precision mediump float;
        #endif

        #extension GL_OES_standard_derivatives : enable

        layout(location = 0) in vec3 vertex;

        out vec2 uv;

        void main(void) {
          gl_Position = vec4(vertex.xy, 0, 1);
          uv = (vertex.xy + 1.0) / 2.0;
        }
      ";
        }

        private string GetFragmentShaderCode()
        {
            return @"
        #version 330 core

        in vec2 uv;
        out vec4 outColor;

        uniform float percentZoomed;
        uniform vec2 resolution;

        void main () {
          float dist = distance(uv.xy, vec2(0.5,0.5));
          float viewStrength = smoothstep(0.45, 0.38, dist * smoothstep(-1, 1, percentZoomed));
          outColor = vec4(0, 0, 0, min(0.8, 1 - viewStrength));
        }
      ";
        }
    }
}
