using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Client;

public class Axis3D {
  readonly int vbo = GL.GenBuffer();
  readonly int vao = GL.GenVertexArray();
  readonly int stride = Marshal.SizeOf<Vertex>();
  readonly Vertex[] vertices = [
    new(new Vector3(0,0,0),new Vector3(1,0,0)),
    new(new Vector3(1,0,0),new Vector3(1,0,0)),

    new(new Vector3(0,0,0),new Vector3(0,1,0)),
    new(new Vector3(0,1,0),new Vector3(0,1,0)),

    new(new Vector3(0,0,0),new Vector3(0,0,1)),
    new(new Vector3(0,0,1),new Vector3(0,0,1))
    ];

  public void Draw() {
    GL.BindVertexArray(vao);
    GL.DrawArrays(PrimitiveType.Lines, 0, 6);
  }

  public Axis3D() {
    GL.BindVertexArray(vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Marshal.SizeOf<Vertex>(), vertices, BufferUsageHint.StaticDraw);

    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

    GL.EnableVertexAttribArray(1);
    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, Marshal.OffsetOf<Vertex>("Color"));

    GL.BindVertexArray(0);
  }
}
