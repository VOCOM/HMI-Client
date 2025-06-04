using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Client.Shaders;

public class Shader {
  readonly int handle;

  public void Use() => GL.UseProgram(handle);
  public void SetUniform(string attributeName, ref Matrix4 matrix) {
    int location = GL.GetUniformLocation(handle, attributeName);
    GL.UniformMatrix4(location, false, ref matrix);
  }

  public Shader() {
    // Compile Vertex Shader
    int vertexShader = GL.CreateShader(ShaderType.VertexShader);
    string vertexSource = File.ReadAllText("Shaders/shader.vert");
    GL.ShaderSource(vertexShader, vertexSource);
    GL.CompileShader(vertexShader);

    // Compile Fragment Shader
    int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
    string fragmentSource = File.ReadAllText("Shaders/shader.frag");
    GL.ShaderSource(fragmentShader, fragmentSource);
    GL.CompileShader(fragmentShader);

    // Link to Program
    handle = GL.CreateProgram();
    GL.AttachShader(handle, vertexShader);
    GL.AttachShader(handle, fragmentShader);
    GL.LinkProgram(handle);

    // Cleanup
    GL.DetachShader(handle, vertexShader);
    GL.DetachShader(handle, fragmentShader);
    GL.DeleteShader(vertexShader);
    GL.DeleteShader(fragmentShader);
  }
}
