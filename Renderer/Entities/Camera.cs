using OpenTK.Mathematics;
using Renderer.Interfaces;
using Renderer.Shaders;

namespace Renderer.Entities;

public class Camera {
  public void Render() {
    Shader.Use();

    var view = Matrix4.LookAt(Position + Offset, Target + Offset, normal);
    Matrix4 proj = TogglePerspective
      ? Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), AspectRatio, Min, Max)
      : Matrix4.CreateOrthographic(Width, Height, Min, Max);
    Shader.SetUniform("uView", ref view);
    Shader.SetUniform("uProjection", ref proj);

    Shader.Render(IRenderable.Entities);
  }

  public bool TogglePerspective { get; set; } = false;
  public float Min { get; set; } = 0.1f;
  public float Max { get; set; } = 100f;
  public float FOV { get; set; } = 90f;
  public float Height { get; set; } = 900f;
  public float Width { get; set; } = 1600f;
  public float AspectRatio => Width / Height;
  public IShader Shader { get; set; } = new Shader();

  public Vector3 Position { get; set; } = Vector3.One;
  public Vector3 Target { get; set; } = Vector3.Zero;
  public Vector3 Offset { get; set; } = Vector3.Zero;

  Vector3 normal = Vector3.UnitY;
}
