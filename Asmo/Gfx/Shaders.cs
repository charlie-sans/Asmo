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
        public const string DefaultVertexShaderSource = @"
#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
out vec2 vTexCoord;
void main()
{
    vTexCoord = aTexCoord;
    gl_Position = vec4(aPosition, 0.0, 1.0);
}

";

        public const string DefaultFragmentShaderSource = @"
#version 330 core
in vec2 vTexCoord;
out vec4 FragColor;
uniform sampler2D uTexture;
void main()
{
    FragColor = texture(uTexture, vTexCoord);
}

";
    }
    // scan line shader for retro effect
    public static class ScanLineShader
    {
        public const string VertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
void main()
{
    gl_Position = vec4(aPos, 1.0);
    TexCoord = aTexCoord;
}
";

        public const string FragmentShaderSource = @"
#version 330 core
out vec4 FragColor;
in vec2 TexCoord;
uniform sampler2D texture1;
void main()
{
    FragColor = texture(texture1, TexCoord);
}
";
    }
}