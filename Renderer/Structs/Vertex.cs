using OpenTK.Mathematics;

namespace Renderer.Structs;

struct Vertex(Vector3 position = default, Color4 color = default) {
  public Vector3 Position = position;
  public Color4 Color = color;
}
