using System;

/// <summary>
/// Shader source code for rendering.
/// this file contains the default built-in shaders as string constants.
/// </summary>
namespace Asmo.Gfx
{
    /// <summary>
    /// Contains shader source code as string constants.
    /// </summary>
    public static class Shaders
    {
        public const string TextVertexShader = "#version 330 core\n"
            + "layout(location = 0) in vec2 aPos;\n"
            + "layout(location = 1) in vec2 aUV;\n"
            + "uniform vec2 uScreenSize;\n"
            + "out vec2 vUV;\n"
            + "void main() {\n"
            + "    vUV = aUV;\n"
            + "    vec2 norm = aPos / uScreenSize * 2.0 - 1.0;\n"
            + "    gl_Position = vec4(norm.x, -norm.y, 0.0, 1.0);\n"
            + "}\n";

        public const string TextFragmentShader = "#version 330 core\n"
            + "in vec2 vUV;\n"
            + "out vec4 FragColor;\n"
            + "uniform sampler2D uFontTex;\n"
            + "uniform vec4 uColor;\n"
            + "void main() {\n"
            + "    float a = texture(uFontTex, vUV).a;\n"
            + "    FragColor = vec4(uColor.rgb, uColor.a * a);\n"
            + "}\n";

        public const string DefaultVertexShaderSource = @"#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
out vec2 vTexCoord;
void main()
{
    vTexCoord = aTexCoord;
    gl_Position = vec4(aPosition, 0.0, 1.0);
}
";

        public const string DefaultFragmentShaderSource = @"#version 330 core
in vec2 vTexCoord;
out vec4 FragColor;
uniform sampler2D uTexture;
void main()
{
    FragColor = texture(uTexture, vTexCoord);
}
";
    }
}