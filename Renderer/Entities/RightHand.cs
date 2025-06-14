using OpenTK.Mathematics;
using Renderer.Interfaces;

namespace Renderer.Entities;

public class RightHand: IRenderable {
  public enum Type {
    Right, Left
  }

  public RightHand() {
    points = [
      new(0.5f),
      new(0.5f),
      new(0.5f),
      new(0.5f),
      new(0.5f),
      new(0.5f)];

    // 1u == ~12cm
    points[0].Position = new();
    points[1].Position = new(-0.5f, 0.35f, 0f);
    points[2].Position = new(-0.25f, 0.9f, 0f);
    points[3].Position = new(0f, 1f, 0f);
    points[4].Position = new(0.25f, 0.9f, 0f);
    points[5].Position = new(0.5f, 0.7f, 0f);

    foreach(Axis3D point in points) IRenderable.Spawn(point);
  }

  public Axis3D this[int index] => points[index];

  readonly List<Axis3D> points;

  // IRenderable Interface
  public void Draw() { }

  public Vector3 Position { get; set; }
  public Quaternion Orientation { get; set; }
  public Matrix4 Matrix { get; set; }
}
