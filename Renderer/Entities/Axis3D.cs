using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Renderer.Interfaces;
using Renderer.Structs;

namespace Renderer.Entities;

public class Axis3D: IRenderable {
  public void Draw() {
    GL.BindVertexArray(vao);
    GL.DrawArrays(PrimitiveType.Lines, 0, 6);
  }

  public Axis3D(float length = 1) {
    vao = GL.GenVertexArray();
    vbo = GL.GenBuffer();
    int stride = Marshal.SizeOf<Vertex>();
    Vertex[] vertices = [
      // X-axis
      new(new(), Color4.Red),
      new(new Vector3(length, 0, 0), Color4.Red),
      // Y-axis
      new(new(), Color4.Green),
      new(new Vector3(0, length, 0), Color4.Green),
      // Z-axis
      new(new(), Color4.Blue),
      new(new Vector3(0,0,length), Color4.Blue)
    ];

    GL.BindVertexArray(vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * stride, vertices, BufferUsageHint.StaticDraw);

    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

    GL.EnableVertexAttribArray(1);
    GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, Marshal.OffsetOf<Vertex>("Color"));

    GL.BindVertexArray(0);
  }

  public Vector3 Position { get; set; } = Vector3.Zero;
  public Quaternion Orientation { get; set; } = Quaternion.Identity;
  public Matrix4 Matrix => Matrix4.CreateFromQuaternion(Orientation)
    * ENU2GL
    * Matrix4.CreateTranslation(Position);

  readonly int vao;
  readonly int vbo;
  readonly Matrix4 ENU2GL = new(Vector4.UnitX, Vector4.UnitZ, Vector4.UnitY, Vector4.UnitW);
}
